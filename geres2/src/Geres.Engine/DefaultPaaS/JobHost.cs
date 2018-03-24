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
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Geres.Util;
using Geres.Common.Interfaces.Engine;
using Geres.Common.Entities;
using Geres.Common.Interfaces.Implementation;
using Geres.Repositories;
using System.Linq;
using System.Linq.Expressions;
using Geres.Repositories.Entities;
using Geres.Common;
using Geres.Common.Entities.Engine;
using System.Collections.Generic;
using Geres.Diagnostics;
using Geres.Repositories.Interfaces;
using Geres.Engine.Util;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Geres.Engine.DefaultPaaS
{
    internal class JobHost : IJobHost
    {
        private string _storageConnectionString;
        private string _serviceBusConnectionString;
        private CloudStorageAccount _storageAccount;
        private CloudQueueClient _queueClient;
        private QueueRequestOptions _requestOptions;

        private IBatchRepository _batchRepository;
        private IJobsRepository _jobsRepository;
        private IJobImplementationFactory _builtInJobsFactory;

        private JobNotificationServiceBus _notificationServiceBusClient;
        private JobCancellationServiceBus _cancellationServiceBusClient;

        private int _messageMaxRetryAttempts = 5;
        private int _messageLockForProcessingInMinutes = 60;

        private bool _singleJobCancellationEnabled = false;
        private TimeSpan _singleJobCancellationTimeWindow = TimeSpan.FromMinutes(10);
        private TimeSpan _singleJobCancellationMessageTimeToLive = TimeSpan.FromSeconds(60);

        private ITenantManager _tenantManager;

        public void Initialize(ITenantManager tenantManager)
        {
            // Set the Tenant Manager
            _tenantManager = tenantManager;

            // Create the factory for built-in jobs
            _builtInJobsFactory = new Geres.Engine.JobFactories.JobWorkerFactory();

            // Read configuration settings
            _storageConnectionString = CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME);
            if (string.IsNullOrEmpty(_storageConnectionString.Trim()))
                throw new Exception("Missing configuration setting " + GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME);
            _serviceBusConnectionString = CloudConfigurationManager.GetSetting(GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME);
            if (string.IsNullOrEmpty(_serviceBusConnectionString.Trim()))
                throw new Exception("Missing configuration setting " + GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME);

            // Parse further configuration settings
            _messageLockForProcessingInMinutes = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.MAX_MESSAGE_LOCK_TIME_IN_MIN));
            _messageMaxRetryAttempts = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.MAX_MESSAGE_RETRY_ATTEMPTS));

            // Read the setting for the single-job-cancellations
            _singleJobCancellationEnabled = bool.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_JOB_SINGLECANCELLATION_ENABLED));
            _singleJobCancellationTimeWindow = TimeSpan.FromSeconds(int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_JOB_SINGLECANCELLATION_TIMEWINDOW_IN_SECONDS)));
            _singleJobCancellationMessageTimeToLive = TimeSpan.FromSeconds(int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_JOB_SINGLECANCELLATION_MESSAGETIMETOLIVE)));

            // Retrieve storage account from connection string.
            _storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            _queueClient = _storageAccount.CreateCloudQueueClient();

            // Create the default batch queue if it does not exist
            var defaultBatchQueue = _queueClient.GetQueueReference(GlobalConstants.DEFAULT_BATCH_ID);
            defaultBatchQueue.CreateIfNotExists();

            // create a retry policy
            _requestOptions = new QueueRequestOptions
            {
                // create a retry policy, retry every 30 seconds for a maximum of 10 times
                RetryPolicy = new LinearRetry(
                    TimeSpan.FromMilliseconds(GlobalConstants.STORAGE_RETRY_MILLISECONDS_BETWEEN_RETRY),
                    GlobalConstants.STORAGE_RETRY_MAX_ATTEMPTS),
            };

            // Create the service bus connection
            // Initialize the connection to Service Bus Queue
            _notificationServiceBusClient = new JobNotificationServiceBus(
                _serviceBusConnectionString,
                    string.Format("{0}_{1}", GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_TOPICPREFIX, GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_JOBSTATUS));

            // Create the service bus connection
            // Initialize the connection to Service Bus Queue
            _cancellationServiceBusClient = new JobCancellationServiceBus(
                _serviceBusConnectionString,
                string.Format("{0}_{1}", GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_TOPICPREFIX, GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_CANCELJOBS),
                _singleJobCancellationTimeWindow,
                _singleJobCancellationMessageTimeToLive);

            // Create all required repositories
            _jobsRepository = RepositoryFactory.CreateJobsRepository(_storageConnectionString);
            _batchRepository = RepositoryFactory.CreateBatchRepository(_storageConnectionString);
        }

        public bool Process(string dedicatedBatchId)
        {
            CloudQueue queue = null;
            OperationContext getMessageContext = new OperationContext();
            OperationContext deleteMessageContext = new OperationContext();

            var jobStatus = JobStatus.Submitted;
            var jobOutput = string.Empty;
            JobProcessResult jobProcessResult = new JobProcessResult();

            //
            // Get the list of batches (equivalet to queues) from table storage and order them based on their priority
            //
            var batches = GetPrioritizedListOfBatches(dedicatedBatchId);

            // 
            // Find a message in the list of batches and process the first message found
            //
            BatchEntity singleBatch = null;
            CloudQueueMessage message = null;
            foreach (var batch in batches)
            {
                // Create the queue client.
                queue = _queueClient.GetQueueReference(batch.Id);

                //
                // Try getting a message from the queue
                //
                try
                {
                    message = TryDequeueMessage(getMessageContext, batch.Id, queue);
                    if (message != null)
                    {
                        singleBatch = batch;
                        GeresEventSource.Log.EngineJobHostReceivedMessage(message.AsString);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    message = null;
                    GeresEventSource.Log.EngineJobHostFailedDequeueMessage(batch.Id, batch.Name, ex.Message, ex.StackTrace);
                    break;
                }
            }
            // Stop processing if there is no message
            if (message == null)
            {
                GeresEventSource.Log.EngineJobHostNoMessageAvailable();
                return false;
            }

            //
            // If there is a message available for processing, try lookup the job for it
            //
            Geres.Common.Entities.Job job = GetJob(message.AsString, singleBatch.Id);
            if (job == null)
            {
                GeresEventSource.Log.EngineJobHostJobForMessageNotFound(message.AsString, singleBatch.Id);
                // Nevertheless return true since the Processor dequeued a message
                return true;
            }
            else if ((job.Status == JobStatus.InProgress) || (job.Status == JobStatus.Started))
            {
                // The job is running, already, on another worker ... skip the job from being executed again
                Geres.Diagnostics.GeresEventSource.Log.EngineJobHostSkipRunningJobSinceItIsStartedAlready(job.JobId, job.JobType, singleBatch.Id);
                // Return true since we dequeued a message from the queue
                return true;
            }
            else
            {
                // Job found, send started message since everything below counts to job processing, already
                TrySendStartServiceBusMessage(job);
            }

            //
            // Try deploying the tenant and the code to process the job to an area on the machine
            // If the path is null then the tenant failed to deploy
            // 
            var tenantJobContext = TryDeployTenant(job, singleBatch.Id);
            if (tenantJobContext == null)
                return true;

            // 
            // Looking up the built-in job processor. There should always be a built-in job processor
            // since the last fall-back will be the job processor worker process
            //
            GeresEventSource.Log.EngineJobHostLookingUpProcessor(string.Format("built-in job processor selection for {0}", job.JobType));
            IJobImplementation processor = _builtInJobsFactory.Lookup(job);
            if (processor == null)
            {
                GeresEventSource.Log.EngineJobHostProcessorLookupFailed("no built-in job processor returned - BUG in system!!");
                return true;
            }
            else
            {
                // Initialize the built-in job if it supports initialization
                if (processor is IJobBuiltInImplementation)
                {
                    ((IJobBuiltInImplementation)processor).InitializeContextBeforeExecution
                        (
                            new BuiltInJobInitializationContext()
                            {
                                ExecutionAsUserName = tenantJobContext.UserName,
                                ExecutionAsUserPassword = tenantJobContext.UserPassword
                            }
                        );
                }
            }
            
            //
            // Job Processor created successfully, hence continue processing
            // Setup a service-bus subscription to allow client-side cancellation
            //
            SubscriptionClient cancellationSubscriptionClient = null;
            string cancellationSubscriptionName = Guid.NewGuid().ToString();
            if (_singleJobCancellationEnabled)
            {
                try
                {
                    cancellationSubscriptionClient = _cancellationServiceBusClient.CreateSubscription(job.JobId, cancellationSubscriptionName);
                    cancellationSubscriptionClient.OnMessage((receivedMessage) =>
                    {
                        processor.CancelProcessCallback();
                    });
                }
                catch (Exception ex)
                {
                    Geres.Diagnostics.GeresEventSource.Log.EngineJobHostFailedSettingUpCancellationSubscriptionForJob(job.JobId, job.JobType, singleBatch.Id, ex.Message, ex.ToString());

                    // Set the job to aborted in the job log
                    try
                    {
                        SetJobMonitoringStatus
                            (
                                job.JobId, singleBatch.Id, JobStatus.AbortedInternalError, string.Empty, true
                            );
                    }
                    catch (Exception exi)
                    {
                        Geres.Diagnostics.GeresEventSource.Log.EngineJobHostFailedUpdatingJobStatus(job.JobId, job.JobName, job.JobType, exi.Message, exi.ToString());
                    }

                    // Job has been dequeued, so return true
                    return true;
                }
            }

            //
            // Now try processing the job
            //
            try
            {
                // Update the status for the job
                SetJobMonitoringStatus(job.JobId, singleBatch.Id, JobStatus.Started, string.Empty, true);

                // simple flag so that the job status table is not constantly updated, i.e. update it once.
                var updateProgress = true;

                GeresEventSource.Log.EngineJobHostStartingJobProcessor(job.JobId, job.JobType, singleBatch.Id);
                // use the newly discovered processor implementation to do the actual work.
                // the callback provides the 3rd party code with the ability to provide progress updates back to the client
                jobProcessResult = processor.DoWork(job, tenantJobContext.JobRootPath, tenantJobContext.JobWorkingRootPath,
                    (unit) =>
                        {
                            TrySendProgressServiceBusMessage(job, unit);

                            if (updateProgress)
                            {
                                SetJobMonitoringStatus(job.JobId, singleBatch.Id, JobStatus.InProgress, string.Empty, true);
                                updateProgress = false;
                            }
                        });

                // Job processing completed, update the status
                jobStatus = jobProcessResult.Status;
                jobOutput = jobProcessResult.Output;

                GeresEventSource.Log.EngineJobHostJobProcessorImplementationCompletedWithStatus(job.JobId, job.JobType, jobStatus.ToString());
            }
            catch (Exception ex)
            {
                // Job Processing failed
                jobStatus = JobStatus.Failed;
                jobOutput = string.Format("Job did run into uncaught exception: {0}!", ex.Message);
                GeresEventSource.Log.EngineJobHostJobProcessorImplementationFailed(job.JobId, job.JobType, singleBatch.Id, ex.Message, ex.StackTrace);
            }

            //
            // notify the originator of the job that processing has finished
            //
            TrySendServiceBusFinishedMessage(jobStatus, jobProcessResult, job);

            // update the status of the job
            try
            {
                SetJobMonitoringStatus(job.JobId, singleBatch.Id, jobStatus, jobOutput, false);
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.EngineJobHostFailedUpdatingJobStatus(job.JobId, job.JobName, job.JobType, ex.Message, ex.StackTrace);
            }

            // delete the cancellation subscription for this jobId
            if(_singleJobCancellationEnabled)
                TryDeleteCacellationServiceBusSubscription(cancellationSubscriptionName, job.JobId, job.JobType);

            // remove the job directory
            _tenantManager.DeleteJobDirectory(job, singleBatch.Id);

            // If the job has any other status but failed, delete the message
            // If the job status is failed and it got dequeued too often, also delete the message
            if (jobStatus != JobStatus.Failed)
            {
                TryDeleteMessage(singleBatch.Id, queue, message);
            }
            else if (message.DequeueCount >= _messageMaxRetryAttempts)
            {
                TryDeleteMessage(singleBatch.Id, queue, message);
            }

            return true;
        }

        #region Repository Helper Methods

        private List<BatchEntity> GetPrioritizedListOfBatches(string dedicatedBatchId)
        {
            List<BatchEntity> batches = null;

            if (!string.IsNullOrEmpty(dedicatedBatchId))
            {
                batches = _batchRepository.GetBatches().Where(b => b.Status.Equals(BatchStatus.Open) && b.Id.Equals(dedicatedBatchId, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }
            else
            {
                // don't consider batches that require dedicated workers
                // these would have already been assigned to a worker
                batches = _batchRepository.GetBatches().Where(b => b.Status.Equals(BatchStatus.Open) && b.RequiresDedicatedWorker.Equals(false)).OrderBy(b => b.Priority).ToList();
            }
            // there should be a least one batch
            if (batches.Count() <= 0)
            {
                var defaultBatch = new BatchEntity()
                {
                    Id = GlobalConstants.DEFAULT_BATCH_ID,
                    Name = GlobalConstants.DEFAULT_BATCH_NAME,
                    Created = DateTime.UtcNow,
                    RequiresDedicatedWorker = false,
                    Priority = 999,
                    Status = BatchStatus.Open
                };

                _batchRepository.CreateBatch(defaultBatch);

                batches.Add(defaultBatch);
            }

            return batches;
        }

        private void SetJobMonitoringStatus(string jobId, string batchId, JobStatus jobStatus, string jobOutput, bool isJobHostOutput)
        {
            GeresEventSource.Log.EngineJobHostUpdatingJobStatus(jobId, batchId, jobStatus.ToString());

            using (var repo = RepositoryFactory.CreateJobsRepository(CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME)))
            {
                var job = repo.GetJob(jobId, batchId);

                job.Status = jobStatus;
                job.JobProcessorLastExecutingInstance = RoleEnvironment.CurrentRoleInstance.Id;

                jobOutput = jobOutput.Trim();
                if (job.JobOutput != null)
                    job.JobOutput = string.Concat(job.JobOutput, Environment.NewLine, Environment.NewLine, jobOutput);
                else
                    job.JobOutput = jobOutput;
                
                if (isJobHostOutput)
                    job.JobOutputSource = "JobHost";
                else
                    job.JobOutputSource = "JobImplementation";

                repo.UpdateJob(job);
            }
        }

        private Job GetJob(string jobId, string batchId)
        {
            GeresEventSource.Log.EngineJobHostLookingUpJobInRepository(jobId, batchId);

            var job = new Job();

            using (var repo = RepositoryFactory.CreateJobsRepository(CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME)))
            {
                var jobEntity = repo.GetJob(jobId, batchId);

                if (jobEntity != null)
                    job = JobEntity.ConvertToJob(jobEntity);
                else
                    throw new ApplicationException("Unable to find job with ID in repository: " + jobId + " and batch ID " + batchId);
            }

            return job;
        }

        #endregion 

        #region Queue Helper Methods

        private CloudQueueMessage TryDequeueMessage(OperationContext getMessageContext, string batchId, CloudQueue queue)
        {
            // each worker will only do one message at a time
            // lock the message based on the configuration setting Geres.JobProcessor.MessageLockTimeInMinutes
            // Note that the interval needs to be configured long enough so that long running jobs don't dequeued again too early although
            // the code later checks if the job is still running, we cannot avoid that the dequeue-count will be increased so subsequent attempts might be skipped due
            // to too often dequeueing the message
            var message = queue.GetMessage
                            (
                                TimeSpan.FromMinutes(_messageLockForProcessingInMinutes),
                                _requestOptions,
                                getMessageContext
                            );

            // If there's no message, return null
            if (message == null) return null;

            // If there is a message and this message has been dequeued too often, flag the job as failed
            if (message.DequeueCount > _messageMaxRetryAttempts)
            {
                GeresEventSource.Log.EngineJobHostMessageDequeueCountExceededError(message.AsString);
                TryDeleteMessage(batchId, queue, message);
                try
                {
                    SetJobMonitoringStatus(message.AsString, batchId, JobStatus.AbortedMaxRetryCount, string.Empty, true);
                }
                catch (Exception ex)
                {
                    GeresEventSource.Log.EngineJobHostFailedUpdatingJobStatus(message.AsString, string.Empty, string.Empty, ex.Message, ex.StackTrace);
                }
                throw new ApplicationException("Message exceeded Dequeue Count - try stop processing message!");
            }

            return message;
        }

        private void TryDeleteMessage(string batchId, CloudQueue queue, CloudQueueMessage message)
        {
            try
            {
                queue.DeleteMessage(message);
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.EngineJobHostFailedDeletingMessage(message.AsString, ex.Message, ex.StackTrace);
            }
        }

        #endregion

        #region Tenant and Job Processor helper methods

        private TenantJobExecutionContext TryDeployTenant(Job job, string batchId)
        {
            TenantJobExecutionContext tenantJobContext = null;

            try
            {
                GeresEventSource.Log.EngineJobHostDeployingTenant(job.JobId, job.JobName, batchId, job.TenantName);
                tenantJobContext = _tenantManager.DeployTenant(job, batchId);
                return tenantJobContext;
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.EngineJobHostDeployingTenantFailed(job.JobId, job.JobName, batchId, job.TenantName, ex.Message, ex.StackTrace);
                try
                {
                    SetJobMonitoringStatus(job.JobId, batchId, JobStatus.AbortedTenantDeploymentFailed, ex.Message, true);
                }
                catch (Exception exi)
                {
                    Geres.Diagnostics.GeresEventSource.Log.EngineJobHostFailedUpdatingJobStatus(job.JobId, job.JobName, job.JobType, exi.Message, exi.StackTrace);
                }

                TrySendServiceBusFinishedMessage(
                    JobStatus.AbortedTenantDeploymentFailed,
                    new JobProcessResult()
                    {
                        Output = ex.Message, 
                        Status = JobStatus.AbortedTenantDeploymentFailed
                    }, 
                    job);

                return null;
            }
        }

        #endregion

        #region Service Bus Helper Methods

        private void TrySendStartServiceBusMessage(Geres.Common.Entities.Job job)
        {
            try
            {
                _notificationServiceBusClient.SendStartedMessage(job);
            }
            catch (Exception ex)
            {
                Geres.Diagnostics.GeresEventSource.Log.EngineJobHostJobProcessorFailedSendingServiceBusMessage(
                        RoleEnvironment.CurrentRoleInstance.Id,
                        RoleEnvironment.DeploymentId,
                        GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_JOBSTATUS,
                        GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBSTARTED,
                        ex.Message,
                        ex.ToString()
                    );
            }
        }

        private void TrySendProgressServiceBusMessage(Geres.Common.Entities.Job job, string progress)
        {
            try
            {
                _notificationServiceBusClient.SendProgressMessage(job, progress);
            }
            catch (Exception ex)
            {
                Geres.Diagnostics.GeresEventSource.Log.EngineJobHostJobProcessorFailedSendingServiceBusMessage(
                        RoleEnvironment.CurrentRoleInstance.Id,
                        RoleEnvironment.DeploymentId,
                        GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_JOBSTATUS,
                        GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBPROGRESS,
                        ex.Message,
                        ex.ToString()
                    );
            }
        }

        private void TrySendServiceBusFinishedMessage(JobStatus jobStatus, JobProcessResult jobProcessResult, Geres.Common.Entities.Job job)
        {
            try
            {
                _notificationServiceBusClient.SendFinishedMessage(job, jobStatus, jobProcessResult.Output);
            }
            catch (Exception ex)
            {
                Geres.Diagnostics.GeresEventSource.Log.EngineJobHostJobProcessorFailedSendingServiceBusMessage(
                        RoleEnvironment.CurrentRoleInstance.Id,
                        RoleEnvironment.DeploymentId,
                        GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_JOBSTATUS,
                        GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBFINISHED,
                        ex.Message,
                        ex.ToString()
                    );
            }
        }

        private void TryDeleteCacellationServiceBusSubscription(string subscriptionName, string jobId, string jobType)
        {
            try
            {
                _cancellationServiceBusClient.DeleteSubscription(subscriptionName);
            }
            catch (Exception ex)
            {
                Geres.Diagnostics.GeresEventSource.Log.EngineJobHostFailedDeletingCancellationSubscriptionForJob
                    (
                        jobId, jobType, subscriptionName, ex.Message, ex.ToString()
                    );
            }
        }

        #endregion
    }
}
