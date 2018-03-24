// <copyright file="WorkerRole.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using ThumbnailShared;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace ThumbnailBackend
{
    public class WorkerRole : RoleEntryPoint
    {
        #region Repository Properties incl. factory

        private ThumbnailQueueRepository _queueRep = null;
        public ThumbnailQueueRepository QueueRepository
        {
            get
            {
                if (_queueRep == null)
                {
                    var storageConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageAccountConfigurationString);
                    _queueRep = new ThumbnailQueueRepository(storageConnectionString);
                }
                return _queueRep;
            }
        }

        private ThumbnailJobRepository _jobsRep = null;
        public ThumbnailJobRepository JobsRepository
        {
            get
            {
                if (_jobsRep == null)
                {
                    var storageConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageAccountConfigurationString);
                    _jobsRep = new ThumbnailJobRepository(storageConnectionString);
                }
                return _jobsRep;
            }
        }

        private ThumbnailBlobRepository _blobRep = null;
        public ThumbnailBlobRepository BlobRepository
        {
            get
            {
                if (_blobRep == null)
                {
                    var storageConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageAccountConfigurationString);
                    _blobRep = new ThumbnailBlobRepository(storageConnectionString);
                }

                return _blobRep;
            }
        }

        #endregion

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("ThumbnailBackend worker role is now running", "Information");

            //
            // Get access to the repository (first call will check if queue and table exists)
            //
            Trace.WriteLine("Getting access to the local repositories...");
            var queueRep = this.QueueRepository;
            var jobsRep = this.JobsRepository;

            // 
            // Now reserve a local temp path where images will be saved by the console app
            //
            Trace.WriteLine("Getting a local directory for temporary work with image thumbnail generation...");
            var localResource = RoleEnvironment.GetLocalResource(SharedConstants.LocalStorageForImageProcessingName);
            
            //
            // Next retrieve the path of the executable deployed with this worker role based on the environment 'RoleRoot' variable 
            // More information: http://blog.toddysm.com/2011/03/what-environment-variables-can-you-use-in-windows-azure.html
            //
            var legacyAppExecutable = System.IO.Path.Combine(
                                                System.Environment.GetEnvironmentVariable("RoleRoot"),
                                                "approot",
                                                "LegacyApp",
                                                "ThumbnailProducerApp.exe"
                                            );

            //
            // Receive messages, download the image, process them with the console app and upload the image to blob
            //

            string jobId = string.Empty;
            bool hasBeenDequeued = false;

            while (true)
            {
                Trace.TraceInformation("Querying queue for new jobs", "Information");

                // Query the queue
                var message = queueRep.GetMessageForJob(out jobId, out hasBeenDequeued);
                if (!string.IsNullOrEmpty(jobId))
                {
                    Trace.WriteLine("Message received from queue...", "Information");

                    // Something has been received from the queue
                    if (hasBeenDequeued)
                    {
                        // The message for the jobId received has been processed 3 times with failure and therefore dequeued --> flag job as failed
                        Trace.WriteLine(string.Format("Removing message for job {0} and flagging job as failed!", jobId), "Warning");
                        FlagJobAsFailed(jobId, true);
                    }
                    else
                    {
                        // There is a new job based on a new message that has not been dequeued --> process it with the legacy app
                        Trace.WriteLine(string.Format("Processing the job {0}", jobId));
                        try
                        {
                            ProcessJob(jobId, legacyAppExecutable, localResource.RootPath);
                            queueRep.DeleteMessage(message);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("Failed processing, flagging job for retry: " + ex.Message);
                            FlagJobAsFailed(jobId, false);
                        }
                    }

                    Trace.WriteLine("Processing completed!", "Information");
                }

                Trace.WriteLine("Waiting 5 sec. for next message!", "Information");
                Thread.Sleep(5000);
            }
        }

        #region Private Processing Functions

        private void ProcessJob(string jobId, string appPath, string localTempPath)
        {
            // 
            // First get the job data
            //
            var job = JobsRepository.GetJob(jobId);
            job.Status = ThumbnailJobEntity.JOB_STATUS_RUNNING;
            JobsRepository.UpdateExistingJob(job);

            //
            // Temporary files used for processing by the legacy app
            //
            var sourceFileName = Path.Combine(localTempPath, "source_" + jobId);
            var targetFileName = Path.Combine(localTempPath, "result_" + jobId);

            try
            {
                // 
                // Next download the source image to the local directory
                //
                var httpClient = new HttpClient();
                var downloadTask = httpClient.GetAsync(job.SourceImageUrl);
                downloadTask.Wait();
                using (var sourceFile = new FileStream(sourceFileName, FileMode.Create))
                {
                    downloadTask.Result.Content.CopyToAsync(sourceFile).Wait();
                }

                // 
                // Then execute the legacy application for creating the thumbnail
                //
                var app = Process.Start
                            (
                                appPath,
                                string.Format("\"{0}\" \"{1}\" Custom 100 100", sourceFileName, targetFileName)
                            );
                // You should set a timeout to wait for the external process and kill if timeout exceeded
                app.WaitForExit();

                // 
                // Evaluate the result of execution and throw exception on failure
                //
                if (app.ExitCode != 0)
                {
                    var errorMessage = string.Format("Legacy app did exit with code {0}, processing failed!", app.ExitCode);
                    Trace.WriteLine(errorMessage, "Warning");
                    throw new Exception(errorMessage);
                }

                //
                // Processing succeeded, Now upload the result file to blob storage and update the jobs table
                // 
                using (FileStream resultFile = new FileStream(targetFileName, FileMode.Open))
                {
                    var resultingUrl = BlobRepository.SaveImageToContainer(resultFile, job.TargetImageName);

                    job.Status = ThumbnailJobEntity.JOB_STATUS_COMPLETED;
                    job.TargetImageUrl = resultingUrl;
                    JobsRepository.UpdateExistingJob(job);
                }
            }
            finally
            {
                //
                // Deletes all temporary files that have been created as part of processing
                // It would also be good to run that time-controlled in regular intervals in case of this fails
                //
                TryCleanUpTempFiles(sourceFileName, targetFileName);
            }
        }

        private void TryCleanUpTempFiles(params string[] filesToCleanUp)
        {
            foreach (var file in filesToCleanUp)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        string.Format
                            (
                                "Unable to clean up file {0} because of {1} - please check if that error continues and ensure recycling of your role!",
                                file, ex.Message
                            ),
                        "Warning"
                    );
                }
            }
        }

        private void FlagJobAsFailed(string jobId, bool finalFailure)
        {
            var job = JobsRepository.GetJob(jobId);
            if (job != null)
            {
                Trace.WriteLine(string.Format("Flagging Job {0} as failed!", jobId));

                if (finalFailure)
                    job.Status = ThumbnailJobEntity.JOB_STATUS_FAILED;
                else
                    job.Status = ThumbnailJobEntity.JOB_STATUS_RETRY;

                JobsRepository.UpdateExistingJob(job);
            }
            else
            {
                Trace.WriteLine(string.Format("No entity found for job {0} in table, skipping...", jobId));
            }
        }

        #endregion
    }
}
