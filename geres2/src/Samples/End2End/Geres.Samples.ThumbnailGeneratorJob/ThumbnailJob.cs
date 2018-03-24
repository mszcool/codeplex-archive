//
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//           http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using Geres.Common;
using Geres.Common.Interfaces.Implementation;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Diagnostics;
using System.IO;

namespace Geres.Samples.ThumbnailGeneratorJob
{
    public class ThumbnailJob : IJobImplementation
    {
        private string _legacyAppExecutable = null;
        private string _localTemporaryPath = null;
        private string _localExecutionPath = null;
        private Process _runningApp = null;

        #region IJobImplementation

        public Common.JobProcessResult DoWork(Common.Entities.Job job, string jobPackagePath, string jobWorkingPath, Action<string> progressCallback)
        {
            //
            // Initialize the job
            //
            InitalizeBasics(jobPackagePath, jobWorkingPath);

            // 
            // Process the actual job
            //
            ProcessJob(job, _localTemporaryPath);

            //
            // Return the result
            //
            return new JobProcessResult()
            {
                Output = "succeeded procesing image " + job.Parameters,
                Status = Common.Entities.JobStatus.Finished
            };
        }

        public string JobType
        {
            get { return "ThumbnailJob"; }
        }

        public void CancelProcessCallback()
        {
            // Try to kill the running app
            try
            {
                if (_runningApp != null)
                {
                    if(!_runningApp.HasExited)
                    {
                        _runningApp.Kill();
                    }
                }
            }
            catch { }

            // Next dispose the app
            try
            {
                _runningApp.Dispose();
            }
            catch { }

            // Next try to clean-up stuff
            try
            {
                TryCleanUpTempFiles();
            } 
            catch { }
        }

        #endregion

        #region Internal Job Logic Methods

        private void InitalizeBasics(string packageDirectory, string workingDirectory)
        {
            Console.WriteLine("Getting a local directory for temporary work with image thumbnail generation...");
            
            _localTemporaryPath = workingDirectory;
            _localExecutionPath = packageDirectory;

            Console.WriteLine("Working with local directories:");
            Console.WriteLine("- Execution: {0}", _localExecutionPath);
            Console.WriteLine("- Working directory: {0}", _localTemporaryPath);

            _legacyAppExecutable = System.IO.Path.Combine(
                                        _localExecutionPath,
                                        "ConsoleLegacyApp",
                                        "ThumbnailProducerApp.exe"
                                    );
        }

        private void ProcessJob(Common.Entities.Job jobDetails, string localTempPath)
        {
            //
            // Temporary files used for processing by the legacy app
            //
            var sourceFileName = Path.Combine(localTempPath, "source_" + jobDetails.JobId);
            var targetFileName = Path.Combine(localTempPath, "result_" + jobDetails.JobId);

            try
            {
                //
                // Parse the parameters passed in for the job
                //
                var parametersFromJob = jobDetails.Parameters.Split('|');
                var storageAccountConnString = parametersFromJob[0];
                var sourceBlobContainerName = parametersFromJob[1];
                var sourceImageName = parametersFromJob[2];
                var targetBlobContainerName = parametersFromJob[3];

                //
                // Create the storage account proxies used for downloading and uploading the image
                //
                var storageAccount = CloudStorageAccount.Parse(storageAccountConnString);
                var blobClient = storageAccount.CreateCloudBlobClient();

                // 
                // Next download the source image to the local directory
                //
                var blobSourceContainer = blobClient.GetContainerReference(sourceBlobContainerName);
                var sourceImage = blobSourceContainer.GetBlobReferenceFromServer(sourceImageName);
                sourceImage.DownloadToFile(sourceFileName, FileMode.Create);

                // 
                // Then execute the legacy application for creating the thumbnail
                //
                _runningApp = Process.Start
                                    (
                                        _legacyAppExecutable,
                                        string.Format("\"{0}\" \"{1}\" Custom 100 100", sourceFileName, targetFileName)
                                    );
                // You should set a timeout to wait for the external process and kill if timeout exceeded
                _runningApp.WaitForExit();

                // 
                // Evaluate the result of execution and throw exception on failure
                //
                if (_runningApp.ExitCode != 0)
                {
                    var errorMessage = string.Format("Legacy app did exit with code {0}, processing failed!", _runningApp.ExitCode);
                    Console.WriteLine(errorMessage, "Warning");
                    throw new Exception(errorMessage);
                }

                //
                // Processing succeeded, Now upload the result file to blob storage and update the jobs table
                // 
                using (FileStream resultFile = new FileStream(targetFileName, FileMode.Open))
                {
                    var blobContainer = blobClient.GetContainerReference(targetBlobContainerName);
                    blobContainer.CreateIfNotExists();

                    // Save it to the targetBlobContainerName with the same name as the original one
                    var blob = blobContainer.GetBlockBlobReference(sourceImageName);

                    // Upload the resulting file to the blob storage
                    blob.UploadFromStream(resultFile);
                }
            }
            finally
            {
                // 
                // No app is running anymore
                //
                try
                {
                    _runningApp.Dispose();
                }
                catch { }
                _runningApp = null;

                //
                // Deletes all temporary files that have been created as part of processing
                // It would also be good to run that time-controlled in regular intervals in case of this fails
                //
                TryCleanUpTempFiles(sourceFileName, targetFileName);
            }
        }

        private void TryCleanUpTempFiles(params string[] filesToCleanUp)
        {
            try
            {
                string[] files = null;

                if (filesToCleanUp.Length == 0)
                {
                    // Get all files from the temp directory
                    files = Directory.GetFiles(_localTemporaryPath);
                }
                else
                {
                    files = filesToCleanUp;
                }

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
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
            catch
            {
                // The job actually does not need to cleanup files since the host environemnt and Geres is doing it. So if this funciton fails, it is not
                // that critical. But instead of flagging the job as failed we leave it and let Geres itself do the clean-up.
            }
        }

        #endregion
    }
}
