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
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using Geres.Util;
using Geres.AutoScaler.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Geres.Repositories;
using Geres.Common.Entities;
using Geres.AutoScaler.Interfaces.AutoScalerPolicy;
using Geres.Repositories.Entities;
using Geres.Repositories.Interfaces;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Geres.AutoScaler.Handlers
{
    internal class AutoScalerHandler : Geres.AutoScaler.Interfaces.IAutoScalerHandler
    {
        private int _checkForJobProcessorCommandsIntervalInSeconds = 60;
        private CloudQueueClient _cloudQueueClient;
        private IAutoScalerPolicy _policy;
        private string _storageConn = string.Empty;
        private string _defaultBatchId = string.Empty;
        private string _serviceBusConn = string.Empty;
        private JobHostServiceBus _jobHostServiceBus;
        private static Object _readyLockingObject = new Object();
        private static Object _idleLockingObject = new Object();
        private CloudQueue _defaultQueue;
        private IJobHostRepository _jobHostRepository;
        private IBatchRepository _batchRepository;
        private IRoleOperationStatusRepository _roleOpsRepository;

        /// <summary>
        /// Initializes all required resources by the AutoSclaer
        /// </summary>
        public void Initialize(IDictionary<string, string> resourceDetails, IAutoScalerPolicy policy)
        {
            if (resourceDetails == null)
                throw new ArgumentException("Argument 'resourceDetails' cannot be null, missing properties for initialization in AutoScaler!");

            //
            // Read configuration settings
            //
            _defaultBatchId = resourceDetails[Constants.QUEUE_HANDLER_INIT_PROP_DEFAULT_BATCH_ID];
            _storageConn = resourceDetails[Constants.QUEUE_HANDLER_INIT_PROP_STORAGECONNECTION];
            _serviceBusConn = resourceDetails[GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME];
            _checkForJobProcessorCommandsIntervalInSeconds = int.Parse(resourceDetails[GlobalConstants.GERES_CONFIG_AUTOSCALER_CHECKFORJOBHOSTUPDATES_INTERVAL]);

            if (string.IsNullOrEmpty(_defaultBatchId) || string.IsNullOrEmpty(_storageConn))
                throw new ArgumentException("Missing properties in resourceDetails parameter!");

            // 
            // Check the loaded policy
            //
            if (policy == null)
                throw new ArgumentException("Missing property for autoscaler policy!");
            _policy = policy;

            //
            // Create all required repositories
            //
            _jobHostRepository = RepositoryFactory.CreateJobHostRepository(_storageConn, RoleEnvironment.DeploymentId);
            _batchRepository = RepositoryFactory.CreateBatchRepository(_storageConn);
            _roleOpsRepository = RepositoryFactory.CreateRoleOperationStatusRepository(_storageConn);

            //
            // Connect to storage for the jobs-queue
            //
            var cloudAccount = CloudStorageAccount.Parse(_storageConn);
            _cloudQueueClient = cloudAccount.CreateCloudQueueClient();
            _defaultQueue = _cloudQueueClient.GetQueueReference(_defaultBatchId);
            _defaultQueue.CreateIfNotExists();

            //
            // Connect to the service bus for autoscaler commands
            //
            _jobHostServiceBus = new JobHostServiceBus(_serviceBusConn, GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORAUTOSCALER, 
                                                                        GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORJOBHOST);
            var subscribeToJobHostCommandsTask = Task.Run(() =>
            {
                SubscriptionClient client = null;

                while (true)
                {
                    try
                    {
                        if (client == null || client.IsClosed)
                        {
                            client = _jobHostServiceBus.CreateSubscription(GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORAUTOSCALER,
                                                                           GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBHOSTUPDATES);
                        }

                        var msg = client.Receive(TimeSpan.FromSeconds(_checkForJobProcessorCommandsIntervalInSeconds));
                        if (msg != null)
                        {
                            if (ProcessCommandFromJobHost(msg))
                                msg.Complete();
                            else
                                msg.Abandon();
                        }
                    }
                    catch (Exception ex)
                    {
                        client = null;
                        Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerAutoScalerListenForJobHostUpdatesFailed
                            (
                                RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId,
                                string.Format("Topic: {0}, Subscription: {1}", GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBHOSTUPDATES,
                                                                               GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORAUTOSCALER),
                                ex.Message,
                                ex.StackTrace
                            );
                    }
                }
            });
        }

        /// <summary>
        /// Performs the actual auto-scaling operation
        /// </summary>
        public void DoAutoScaling()
        {
            var jobHostCount = 0;
            var batchQueues = new List<CloudQueue>();

            // Getting the number of JobHosts registered in the repository
            jobHostCount = _jobHostRepository.GetJobHosts().Where(jh => !jh.Status.Equals(JobHostStatus.Idle)).Count();

            // get the batch queues but keep them separate to the default queue
            _batchRepository.GetBatches()
                           .Where(b => b.Id != _defaultBatchId && b.Status.Equals(BatchStatus.Open))
                           .ToList()
                           .ForEach(b => batchQueues.Add(_cloudQueueClient.GetQueueReference(b.Id)));

            // Calculate the delta between available instances and required instances as per the loaded policy
            int delta = this._policy.DoScaleOut(_defaultQueue, batchQueues, jobHostCount);

            // do we need to increase the number of hosts to meet the demand of the job queues?
            if (delta > 0)
            {
                delta = NotifyIdleWorkersToStart(delta);
            }

            // Perform adding instances or removing instances as required
            bool canDoRoleOperations = CanDoRoleOperations();
            if (canDoRoleOperations)
            {
                // once all the idle workers have been used is there a need to create new role instances 
                if (delta > 0)
                {
                    if (RoleEnvironment.IsEmulated == false)
                        canDoRoleOperations = AddNewInstancesAsRequired(delta, canDoRoleOperations);
                }
                else
                {
                    // do we need to remove idle workers? 
                    // based on the policy - remove idle workers that have been idle for a given timespan
                    IEnumerable<JobHostEntity> overIdleJobHosts = _jobHostRepository.GetJobHosts().Where(jh => jh.Status.Equals(JobHostStatus.Idle) && DateTimeOffset.Now.Subtract(jh.Timestamp) > _policy.IdleTime).ToList();
                    var idleCount = overIdleJobHosts.Count();
                    if (idleCount > 0)
                    {
                        overIdleJobHosts = RemoveIdleInstancesAsNeeded(overIdleJobHosts, idleCount);
                    }
                }
            }
        }

        /// <summary>
        /// Notifies all IDLE workers according to the infrastructure table to start working
        /// </summary>
        private int NotifyIdleWorkersToStart(int delta)
        {
            // are there any idle workers? If so then use these before adding new instances.
            var jobHostEntities = _jobHostRepository.GetJobHosts().Where(j => j.Status.Equals(JobHostStatus.Idle));

            foreach (var jobHostEntity in jobHostEntities)
            {
                // re-assign the status of the jobhost to running
                jobHostEntity.Status = JobHostStatus.Run;
                _jobHostRepository.UpdateJobHost(jobHostEntity);

                // send a command to the
                _jobHostServiceBus.SendJobHostUpdateMessage(new JobHost
                {
                    RoleInstanceId = jobHostEntity.RoleInstanceId,
                    Status = jobHostEntity.Status,
                    DedicatedBatchId = jobHostEntity.DedicatedBatchId,
                    Id = jobHostEntity.Id,
                    DeploymentId = jobHostEntity.DeploymentId
                });

                delta--;

                // if the increase in roles has been satisfied then break
                if (delta.Equals(0))
                    break;
            }
            return delta;
        }

        /// <summary>
        /// Adds new instances depending on the calculated delta and if role operations are possible
        /// </summary>
        private bool AddNewInstancesAsRequired(int delta, bool canDoRoleOperations)
        {
            // Again get the number of registered Job Processors in the table
            var jobHostCount = _jobHostRepository.GetJobHosts().Count();

            // how many hosts 'in total' do we need?
            var newCount = (delta + jobHostCount > _policy.MaximumJobHosts) ? _policy.MaximumJobHosts : (delta + jobHostCount);

            string requestId = ManagementApiHelper.AddJobHostInstances(GlobalConstants.JOBTASK_PROCESS_ROLENAME, newCount);

            if (!string.IsNullOrEmpty(requestId))
            {
                UpdateRoleOperationStatus(requestId);
                canDoRoleOperations = false;

                for (int i = 1; i <= newCount - jobHostCount; i++)
                {
                    _jobHostRepository.CreateJobHost(new JobHostEntity());
                }
            }
            return canDoRoleOperations;
        }

        /// <summary>
        /// Removes Job Processor workers which are IDLE for a while
        /// </summary>
        private IEnumerable<JobHostEntity> RemoveIdleInstancesAsNeeded(IEnumerable<JobHostEntity> overIdleJobHosts, int idleCount)
        {
            // does the policy state a minumum number of idle workers?
            overIdleJobHosts = (idleCount > _policy.MaximumIdleJobHosts) ? overIdleJobHosts.Take(idleCount - _policy.MaximumIdleJobHosts) : overIdleJobHosts.Take(0);

            string[] roleInstances = overIdleJobHosts.Select(jhe => jhe.RoleInstanceId).ToArray<string>();

            if (overIdleJobHosts.Count() > 0)
            {
                string requestId = string.Empty;

                if (RoleEnvironment.IsEmulated == false)
                    requestId = ManagementApiHelper.RemoveJobHostInstances(roleInstances);

                if (!string.IsNullOrEmpty(requestId))
                {
                    UpdateRoleOperationStatus(requestId);

                    // mark the jobHosts as deleting
                    foreach (var jhe in overIdleJobHosts)
                    {
                        jhe.Status = JobHostStatus.Deleting;
                        _jobHostRepository.UpdateJobHost(jhe);
                    }
                }
            }
            return overIdleJobHosts;
        }

        /// <summary>
        /// Updates the status of role operations in a tracking table of the AutoScaler
        /// </summary>
        private void UpdateRoleOperationStatus(string requestId)
        {
            // update the RoleOperationEntity
            var roleOperation = _roleOpsRepository.GetRoleOperationStatus();

            if (roleOperation != null)
            {
                roleOperation.RequestId = requestId;
                _roleOpsRepository.UpdateRoleOperationStatus(roleOperation);
            }
            else
            {
                roleOperation = new RoleOperationStatusEntity() { RequestId = requestId };
                _roleOpsRepository.CreateRoleOperationStatus(roleOperation);
            }
        }

        /// <summary>
        /// Verifies if there's another role operartion going on and therefore we cannot do further role operations
        /// </summary>
        private bool CanDoRoleOperations()
        {
            // we can only add / remove instances if there is no role operation currently executing
            // read the RoleOperationStatusEntity and find out if there is a outstanding requestId
            // if there is then check the progress of the request.
            // if the request is complete then do the next operation

            bool canDoRoleOperation = true;

            var roleOperation = _roleOpsRepository.GetRoleOperationStatus();

            if (roleOperation != null)
            {
                if (string.IsNullOrEmpty(roleOperation.RequestId) == false)
                {
                    var status = ManagementApiHelper.CheckProgress(roleOperation.RequestId);

                    if (!status.Equals("InProgress", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // there should not be any reference to deleting (preparing items will be cleaned up when the role instances are ready)
                        if (status.Equals("Succeeded", StringComparison.CurrentCultureIgnoreCase))
                        {
                            var jobHosts = _jobHostRepository.GetJobHosts().Where(jh => jh.Status.Equals(JobHostStatus.Deleting)).ToList();

                            jobHosts.ForEach(jh => _jobHostRepository.DeleteJobHost(jh.RoleInstanceId));
                        }

                        // there should not be any reference to preparing or deleted
                        if (status.Equals("Failed", StringComparison.CurrentCultureIgnoreCase))
                        {
                            var prepJobHosts = _jobHostRepository.GetJobHosts().Where(jh => jh.Status.Equals(JobHostStatus.Preparing)).ToList();

                            prepJobHosts.ForEach(jh => _jobHostRepository.DeleteJobHost(jh.RoleInstanceId));

                            // put them back to idle - the delete will try again next time round
                            var deleteJobHosts = _jobHostRepository.GetJobHosts().Where(jh => jh.Status.Equals(JobHostStatus.Deleting)).ToList();

                            deleteJobHosts.ForEach(jh =>
                            {
                                jh.Status = JobHostStatus.Idle;
                                _jobHostRepository.UpdateJobHost(jh);
                            });
                        }

                        // update the RoleOperationStatus so that the requestId is empty
                        roleOperation.RequestId = string.Empty;
                        _roleOpsRepository.UpdateRoleOperationStatus(roleOperation);
                    }
                    else
                    {
                        canDoRoleOperation = false;
                    }
                }
                else
                {
                    // create an entry
                    _roleOpsRepository.CreateRoleOperationStatus(new RoleOperationStatusEntity());
                }
            }

            return canDoRoleOperation;
        }

        /// <summary>
        /// Process a command received from the job host
        /// </summary>
        /// <param name="msg"></param>
        private bool ProcessCommandFromJobHost(Microsoft.ServiceBus.Messaging.BrokeredMessage msg)
        {
            JobHost jobHost = null;
            try
            {
                var senderRoleInstanceId = "unknown";
                try
                {
                    senderRoleInstanceId = msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_ROLEINSTANCEID].ToString();
                }
                catch
                {
                    senderRoleInstanceId = "failed retrieving sender from message";
                }

                Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerReceivedCommandFromJobHost
                    (
                        RoleEnvironment.CurrentRoleInstance.Id,
                        RoleEnvironment.DeploymentId,
                        msg.MessageId,
                        senderRoleInstanceId
                    );

                jobHost = msg.GetBody<JobHost>();
                ManageJobHost(jobHost);

                // Processed successfully
                return true;
            }
            catch (Exception ex)
            {
                string jobHostId = "unknown";
                if (jobHost != null)
                    if (!string.IsNullOrEmpty(jobHost.RoleInstanceId))
                        jobHostId = jobHost.RoleInstanceId;

                Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerFailedManageJobHost(
                        RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, jobHostId, ex.Message, ex.StackTrace);

                // Failed processing
                return false;
            }
        }

        /// <summary>
        /// Updates the infrastructure tables based on notifications received from running workers.
        /// </summary>
        private void ManageJobHost(JobHost jobHost)
        {
            Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerProcessingJobHostCommand(jobHost.RoleInstanceId, jobHost.Id, jobHost.DedicatedBatchId, jobHost.Status.ToString());

            switch (jobHost.Status)
            {
                case JobHostStatus.Idle:
                    {
                        lock (_idleLockingObject)
                        {
                            // should not go idle if the number of actively running instances has not been reached
                            int runningCount = _jobHostRepository.GetJobHosts().Where(jh => jh.Status.Equals(JobHostStatus.Run)).Count();

                            if (runningCount <= _policy.MinimumRunningJobHosts)
                            {
                                jobHost.Status = JobHostStatus.Run;
                                Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerSendingStartRunningCommand(jobHost.RoleInstanceId, jobHost.Id, jobHost.DedicatedBatchId, jobHost.Status.ToString());
                                _jobHostServiceBus.SendJobHostUpdateMessage(jobHost);
                                Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerSuccessfullySentStartRunningCommand(jobHost.RoleInstanceId, jobHost.Id, jobHost.DedicatedBatchId, jobHost.Status.ToString());
                            }

                            // notify the jobhost
                            _jobHostRepository.UpdateJobHost(new JobHostEntity(jobHost));
                        }
                    };

                    break;

                case JobHostStatus.Ready:
                    {
                        // the worker role is now ready to read queues
                        // send the worker a notification so that they can start checking the queue
                        jobHost.Status = JobHostStatus.Run;

                        // update the topology to know show this worker instance is now ready and is reading the queues
                        // lock this process 
                        lock (_readyLockingObject)
                        {
                            // find a preparing jobhost record and replace
                            var prepJobHost = _jobHostRepository.GetJobHosts().Where(j => j.Status.Equals(JobHostStatus.Preparing)).FirstOrDefault();

                            if (prepJobHost != null)
                            {
                                _jobHostRepository.DeleteJobHost(prepJobHost.RoleInstanceId);
                                _jobHostRepository.CreateJobHost(new JobHostEntity(jobHost));
                            }
                            else
                            {
                                // does it already exist
                                var existJobHost = _jobHostRepository.GetJobHost(jobHost.RoleInstanceId);

                                if (existJobHost != null)
                                {
                                    _jobHostRepository.DeleteJobHost(existJobHost.RoleInstanceId);
                                }

                                _jobHostRepository.CreateJobHost(new JobHostEntity(jobHost));
                            }
                        }

                        Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerSendingStartRunningCommand(jobHost.RoleInstanceId, jobHost.Id, jobHost.DedicatedBatchId, jobHost.Status.ToString());
                        _jobHostServiceBus.SendJobHostUpdateMessage(jobHost);
                        Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerSuccessfullySentStartRunningCommand(jobHost.RoleInstanceId, jobHost.Id, jobHost.DedicatedBatchId, jobHost.Status.ToString());
                    };

                    break;
            }
        }
    }
}
