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
using Geres.Engine.Util;
using Geres.Common.Entities;
using Geres.Common.Interfaces.Engine;
using Geres.Diagnostics;
using Geres.Repositories;
using Geres.Util;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Claims;

namespace Geres.Engine.DefaultPaaS
{
    internal class JobController : IJobController
    {
        private CloudQueue _sharedQueue;
        private CloudQueueClient _queueClient;
        private string _storageConnectionString;
        private string _serviceBusConnectionString;
        private string _cancellationServiceBusTopicName;

        #region IJobController Implementation

        /// <summary>
        /// Performs initialization of storage access etc.
        /// </summary>
        public virtual void Initialize()
        {
            // Get the storage connection string
            _storageConnectionString = CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME);
            if (string.IsNullOrEmpty(_storageConnectionString.Trim()))
                throw new Exception("Missing Storage Connection String " + GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME);

            _serviceBusConnectionString = CloudConfigurationManager.GetSetting(GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME);
            if (string.IsNullOrEmpty(_serviceBusConnectionString.Trim()))
                throw new Exception("Missing ServiceBus Connection String " + GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME);

            _cancellationServiceBusTopicName = string.Format("{0}_{1}", GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_TOPICPREFIX, GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_CANCELJOBS);

            // Create the queue client for processing the queues of the different batches and create the default-batch queue
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            _queueClient = storageAccount.CreateCloudQueueClient();
            _queueClient.RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.LinearRetry(
                TimeSpan.FromSeconds(GlobalConstants.STORAGE_RETRY_MILLISECONDS_BETWEEN_RETRY),
                GlobalConstants.STORAGE_RETRY_MAX_ATTEMPTS);
            _sharedQueue = _queueClient.GetQueueReference(GlobalConstants.DEFAULT_BATCH_ID);
            if (!_sharedQueue.Exists()) _sharedQueue.Create();

            // add the shared queue info to the table storage if it doesn't exist
            using (var repo = RepositoryFactory.CreateBatchRepository(_storageConnectionString))
            {
                if (repo.GetBatches().Where(b => b.Id.Equals(GlobalConstants.DEFAULT_BATCH_ID)).FirstOrDefault() == null)
                {
                    var batch = new Batch();

                    batch.Id = GlobalConstants.DEFAULT_BATCH_ID;
                    batch.BatchName = GlobalConstants.DEFAULT_BATCH_NAME;

                    batch.Status = BatchStatus.Open;
                    batch.Priority = 999;

                    repo.CreateBatch(new Repositories.Entities.BatchEntity(batch));
                }
            }
        }

        /// <summary>
        /// A batch of jobs is ready for processing - create a dedicated queue for the batch
        /// </summary>
        public Batch CreateBatch(Batch newBatch)
        {
            // create an identifier for the batch
            newBatch.Id = Guid.NewGuid().ToString();
            newBatch.Status = BatchStatus.Open;

            // create a new queue for this batch
            var batchQueue = _queueClient.GetQueueReference(newBatch.Id);
            batchQueue.CreateIfNotExists();

            // add the queue info to the table storage
            using (var repo = RepositoryFactory.CreateBatchRepository(_storageConnectionString))
            {
                repo.CreateBatch(new Repositories.Entities.BatchEntity(newBatch));
            }

            return newBatch;
        }

        /// <summary>
        /// Cancel any further processing of jobs for this batch
        /// Send a cancel notification to halt the processing of jobs
        /// </summary>
        public void CancelBatch(string batchId)
        {
            // cannot close the shared queue
            if (IsSharedQueue(batchId))
                throw new InvalidOperationException(GlobalConstants.SHARED_QUEUE_CANNOT_BE_CANCELLED);

            var batchQueue = _queueClient.GetQueueReference(batchId);

            // change the status of the queue in table storage to stop the addition of new jobs
            // the new status will be closed.
            MarkBatchAsClosed(batchId);

            // delete the queue
            batchQueue.DeleteIfExists();

            // Cancel all jobs belonging to the batch which are running, already
            var jobIds = new List<string>();
            string pagingToken = null;
            using (var repo = RepositoryFactory.CreateJobsRepository(_storageConnectionString))
            {
                do
                {
                    var jobsInRepo = repo.GetJobs(batchId, 1000, ref pagingToken);
                    jobIds.AddRange
                        (
                            jobsInRepo.Where(j => j.Status == JobStatus.Started || j.Status == JobStatus.InProgress)
                                      .Select(j => j.JobId)
                                      .ToList()
                       );
                } while (!string.IsNullOrEmpty(pagingToken));
            }

            // Send a cancel notification to the Cancel Topic, but only if the topic exists (means cancellation is enabled)
            if(JobCancellationServiceBus.CancellationTopicExists(_serviceBusConnectionString, _cancellationServiceBusTopicName))
                jobIds.ForEach(j => SendCancellationNotification(j));
        }

        /// <summary>
        /// Close the batch by deleting the queue or marking the queue as 'ready' for deleting
        /// </summary>
        public virtual void CloseBatch(string batchId)
        {
            // cannot close the shared queue
            if (IsSharedQueue(batchId))
                throw new InvalidOperationException(GlobalConstants.SHARED_QUEUE_CANNOT_BE_CLOSED);

            var batchQueue = _queueClient.GetQueueReference(batchId);

            // change the status of the queue in table storage to stop the addition of new jobs - the new status will be closed.
            MarkBatchAsClosed(batchId);

            if (batchQueue.Exists())
            {
                // if the queue length is zero then delete the queue
                batchQueue.FetchAttributes();

                // can the queue be deleted
                var deleteQueue = true;

                // the approximate message includes getMessages and getMessage but not deleteMessage
                if (batchQueue.ApproximateMessageCount.HasValue)
                {
                    var messageCount = batchQueue.ApproximateMessageCount.Value;

                    if (messageCount != 0)
                        deleteQueue = false;
                }

                if (deleteQueue)
                {
                    batchQueue.DeleteIfExists();
                }
            }
        }

        /// <summary>
        /// Allows submitting a job to the system. If no batch is specified, the default batch is used.
        /// </summary>
        public virtual string SubmitJob(Job job, string batchId)
        {
            if (!string.IsNullOrEmpty(batchId))
                if (!IsBatchQueueOpen(batchId))
                    throw new InvalidOperationException(GlobalConstants.BATCH_IS_CLOSED_EXCEPTION);

            return SubmitJobToQueue(job, batchId);
        }

        /// <summary>
        /// Allows submitting a list of jobs. If no batch is specified, the default batch is used.
        /// </summary>
        public virtual List<string> SubmitJobs(List<Job> jobs, string batchId)
        {
            if (!string.IsNullOrEmpty(batchId))
                if (!IsBatchQueueOpen(batchId))
                    throw new InvalidOperationException(GlobalConstants.BATCH_IS_CLOSED_EXCEPTION);

            var jobIds = new List<string>();

            jobs.ForEach(j =>
            {
                jobIds.Add(SubmitJobToQueue(j, batchId));
            });

            return jobIds;
        }

        /// <summary>
        /// Allows to cancel a running job by its job id.
        /// </summary>
        public virtual void CancelJob(string jobId)
        {
            if(JobCancellationServiceBus.CancellationTopicExists(_serviceBusConnectionString, _cancellationServiceBusTopicName))
                SendCancellationNotification(jobId);
        }

        #endregion

        #region Private helper methods

        /// <summary>
        /// Marks a batch as closed so that no further jobs are accepted, anymore
        /// </summary>
        private void MarkBatchAsClosed(string batchId)
        {
            using (var repo = RepositoryFactory.CreateBatchRepository(_storageConnectionString))
            {
                var batch = repo.GetBatches().Where(b => b.Id.Equals(batchId)).FirstOrDefault();

                if (batch != null)
                {
                    batch.Status = BatchStatus.Closed;
                }

                repo.UpdateBatch(batch);
            }
        }

        /// <summary>
        /// Sends the cancellation notification through service bus to workers to stop a job in progress.
        /// </summary>
        private void SendCancellationNotification(string jobId)
        {
            // add a new notification to the cancel topic
            JobCancellationServiceBus.SendCancellationMessage
                (
                    jobId, 
                    _serviceBusConnectionString, 
                    _cancellationServiceBusTopicName
                );
        }

        /// <summary>
        /// Determines if the batch queue is available (not closed) or not.
        /// </summary>
        private bool IsBatchQueueOpen(string batchId)
        {
            var result = false;

            using (var repo = RepositoryFactory.CreateBatchRepository(_storageConnectionString))
            {
                var batch = repo.GetBatches().Where(b => b.Id.Equals(batchId)).FirstOrDefault();

                if (batch != null)
                {
                    if (batch.Status == BatchStatus.Open)
                        result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether the batch is the default batch or not
        /// </summary>
        private bool IsSharedQueue(string batchId)
        {
            bool result = false;

            if (batchId.Equals(GlobalConstants.DEFAULT_BATCH_ID))
                result = true;

            return result;
        }

        /// <summary>
        /// Gets the name identifier claim for the currently assigned identity on the context.
        /// </summary>
        private string GetCurrentIdentityName()
        {
            var claimsIdentity = System.Threading.Thread.CurrentPrincipal.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                var upnClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Upn);
                if (upnClaim != null)
                {
                    // This should be available for users signed in with Azure AD
                    return upnClaim.Value;
                }
                else
                {
                    // This should be availble for service credentials signed in with Azure AD (client-id)
                    var nameIdClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (nameIdClaim != null)
                        return nameIdClaim.Value;
                    else
                        throw new SecurityException("Neither the UPN-claim nor the NameIdentifier-claim was present in the submitted token. Make sure these claims are issued by your IdP!");
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Internal mechanism to handle jobs being put on the queue
        /// </summary>
        private string SubmitJobToQueue(Job job, string batchId)
        {
            // Log that we're trying to submit a job
            GeresEventSource.Log.EngineJobControllerTrySubmittingJob(job.JobName, job.JobType, batchId);

            // Initialize the properties of the job
            job.JobId = Guid.NewGuid().ToString();
            job.SubmittedAt = DateTime.UtcNow;
            job.ScheduledBy = GetCurrentIdentityName();
            job.Status = JobStatus.Submitted;
            job.JobProcessorPackageName = job.JobProcessorPackageName;
            job.TenantName = job.TenantName;

            // Log the submission of the Message
            GeresEventSource.Log.EngineJobControllerSubmittingJobToQueue
                (
                    job.JobId, job.JobName, job.JobType, job.SubmittedAt, job.ScheduledBy, batchId, job.JobProcessorPackageName, false
                );

            // the message is simply the id of the 
            var message = new CloudQueueMessage(job.JobId);

            CloudQueue queue = null;
            string batchName = "";

            // Determine which queue to use
            if (!string.IsNullOrEmpty(batchId))
            {
                // use a custom queue if the batch does exist
                using (var repo = RepositoryFactory.CreateBatchRepository(_storageConnectionString))
                {
                    var batchExisting = repo.GetBatch(batchId);
                    if (batchExisting == null)
                        throw new InvalidOperationException(GlobalConstants.BATCH_DOES_NOT_EXIST_EXCEPTION);
                    else batchName = batchExisting.Name;
                }

                // Create the queue for the batch if it does not exist
                queue = _queueClient.GetQueueReference(batchId);
                queue.CreateIfNotExists();
            }
            else
            {
                // use Q0
                queue = _sharedQueue;
                batchId = GlobalConstants.DEFAULT_BATCH_ID;
                batchName = GlobalConstants.DEFAULT_BATCH_NAME;
            }

            // Update the repository with the job
            try
            {
                using (var repo = RepositoryFactory.CreateJobsRepository(_storageConnectionString))
                {
                    var jobEntity = new Repositories.Entities.JobEntity(job);
                    jobEntity.BatchId = batchId;
                    jobEntity.BatchName = batchName;
                    repo.CreateJob(jobEntity);
                }
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.EngineJobControllerFailedUpdatingJob(job.JobId, job.JobName, job.JobType, ex.Message, ex.StackTrace);
                throw new ApplicationException(GlobalConstants.SUBMIT_JOB_FAILED, ex);
            }

            // Try submitting the Job to the Queue
            try
            {
                queue.AddMessage(message);
                GeresEventSource.Log.EngineJobControllerSubmittedToBatch(job.JobId, job.JobName, job.JobType, batchId, batchName);
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.EngineJobControllerSubmittingToBatchQueueFailed(job.JobId, job.JobName, job.JobType, batchId, batchName, ex.Message, ex.StackTrace);
                try
                {
                    using (var repo = RepositoryFactory.CreateJobsRepository(_storageConnectionString))
                    {
                        job.Status = JobStatus.Failed;
                        repo.UpdateJob(new Repositories.Entities.JobEntity(job));
                    }
                }
                catch (Exception exi)
                {
                    GeresEventSource.Log.EngineJobControllerFailedUpdatingJob(job.JobId, job.JobName, job.JobType, exi.Message, exi.StackTrace);
                }

                throw new ApplicationException(GlobalConstants.SUBMIT_JOB_FAILED, ex);
            }

            return job.JobId;
        }

        #endregion
    }
}
