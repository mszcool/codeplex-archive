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
using System;
using Geres.Common.Entities;
using Geres.Repositories;
using Geres.Repositories.Entities;
using Geres.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Newtonsoft.Json;
using Geres.Azure.PaaS.JobHub.Hubs;
using Geres.Diagnostics;
using System.Text;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Geres.Azure.PaaS.JobHub
{
    public class JobNotificationServiceBusSignalRBridge
    {
        private readonly string _connectionString;
        private readonly string _notificationTopicName;
        private ProgressNotificationHandler _signalRNotificationHandler;

        public JobNotificationServiceBusSignalRBridge()
        {
            GeresEventSource.Log.JobHubSignalRServiceBusBridgeReadConfig(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
            _connectionString = CloudConfigurationManager.GetSetting(GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME);
            _notificationTopicName = string.Format("{0}_{1}", GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_TOPICPREFIX, GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_JOBSTATUS);
            GeresEventSource.Log.JobHubSignalRServiceBusBridgeReadConfigSuccessful(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
        }

        private void InitializeTopic()
        {
            GeresEventSource.Log.JobHubSignalRServiceBusBridgeInitializingTopicsAndSubscriptions(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);

            var namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);
            namespaceManager.Settings.RetryPolicy = RetryPolicy.Default;

            if (!namespaceManager.TopicExists(_notificationTopicName))
            {
                GeresEventSource.Log.JobHubSignalRServiceBusBridgeCreatingTopic(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, _notificationTopicName);

                try
                {
                    var desc = new TopicDescription(_notificationTopicName) { };
                    namespaceManager.CreateTopic(desc);

                    GeresEventSource.Log.JobHubSignalRServiceBusBridgeCreateTopicSuccessful(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, _notificationTopicName);
                }
                catch (Exception ex)
                {
                    GeresEventSource.Log.JobHubSignalRServiceBusBridgeTopicCreationFailed(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, _notificationTopicName, ex.Message, ex.StackTrace);
                }
            }

            // Should we add more subscriptions or extend the filter of the Finished Subscription to include the above.
            try
            {
                GeresEventSource.Log.JobHubSignalRServiceBusBridgeCreatingSubscriptions(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);

                CreateSubscriptionIfNotExists(namespaceManager, GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBSTARTED,
                    GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS, JobStatus.Started.ToString());

                CreateSubscriptionIfNotExists(namespaceManager, GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBFINISHED,
                    GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS, JobStatus.Finished.ToString(), 
                                                                       JobStatus.Aborted.ToString(),
                                                                       JobStatus.AbortedInternalError.ToString(),
                                                                       JobStatus.AbortedJobProcessorMissingOrFailedLoading.ToString(),
                                                                       JobStatus.AbortedTenantDeploymentFailed.ToString(),
                                                                       JobStatus.Failed.ToString(),
                                                                       JobStatus.FailedUnexpectedly.ToString());

                CreateSubscriptionIfNotExists(namespaceManager, GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBPROGRESS,
                    GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS, JobStatus.InProgress.ToString());

                CreateSubscriptionIfNotExists(namespaceManager, GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBCANCELLED,
                    GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS, JobStatus.Cancelled.ToString());

                GeresEventSource.Log.JobHubSignalRServiceBusBridgeCreatingSubscriptionsSuccessful(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.JobHubSignalRServiceBusBridgeCreateSubscriptionsFailed(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, ex.Message, ex.StackTrace);
            }

            GeresEventSource.Log.JobHubSignalRServiceBusBridgeTopicsAndSubscriptionsInitializedSuccessful(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
        }

        private void CreateSubscriptionIfNotExists(NamespaceManager namespaceManager,
            string subscriptionName, string messageProperty, string filterValue, params string[] moreFilterValues)
        {
            if (!namespaceManager.SubscriptionExists(_notificationTopicName, subscriptionName))
            {
                StringBuilder filter = new StringBuilder();
                filter.AppendFormat("{0} = '{1}'", messageProperty, filterValue);
                if (moreFilterValues != null && moreFilterValues.Length > 0)
                {
                    foreach (var f in moreFilterValues)
                    {
                        filter.AppendFormat(" OR {0} = '{1}'", messageProperty, f);
                    }
                }

                namespaceManager.CreateSubscription(_notificationTopicName, subscriptionName,
                    new SqlFilter(filter.ToString()));
            }
        }

        public void RunSignalRBridgeLoop()
        {
            InitializeTopic();

            var hub = GlobalHost.ConnectionManager
                .GetHubContext<SignalRNotificationHub>();

            SubscriptionClient startedJobsClient =
                SubscriptionClient.CreateFromConnectionString(_connectionString,
                    _notificationTopicName,
                    GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBSTARTED);

            SubscriptionClient inProgressJobsClient =
                SubscriptionClient.CreateFromConnectionString(_connectionString,
                    _notificationTopicName,
                    GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBPROGRESS);

            SubscriptionClient finishedJobsClient =
                SubscriptionClient.CreateFromConnectionString(_connectionString,
                    _notificationTopicName,
                    GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBFINISHED);

            SubscriptionClient cancelledJobsClient =
                SubscriptionClient.CreateFromConnectionString(_connectionString,
                    _notificationTopicName,
                    GlobalConstants.SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBCANCELLED);            

            _signalRNotificationHandler = new ProgressNotificationHandler();

            startedJobsClient.OnMessage(HandleNotificationMessage);
            inProgressJobsClient.OnMessage(HandleNotificationMessage);
            finishedJobsClient.OnMessage(HandleNotificationMessage);
            cancelledJobsClient.OnMessage(HandleNotificationMessage);
        }

        private void HandleNotificationMessage(BrokeredMessage msg)
        {
            try
            {
                GeresEventSource.Log.JobHugSignalRServiceBusBridgeNotificationReceived(msg.MessageId);

                var job = msg.GetBody<Job>();

                GeresEventSource.Log.JobHubSignalRServiceBusBridgeNotificationReceivedJobParsed(job.JobId, job.JobType, job.Status.ToString());

                var status =
                        (JobStatus)
                            Enum.Parse(typeof(JobStatus),
                                (string)msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS]);

                job.Status = status;

                switch (status)
                {
                    case JobStatus.Started:
                        _signalRNotificationHandler.PublishJobStart(job);
                        break;

                    case JobStatus.InProgress:
                        _signalRNotificationHandler.PublishJobProgress(job,
                            (string)msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_PROGRESSUNIT]);
                        break;

                    default:
                        _signalRNotificationHandler.PublishJobComplete(job);
                        break;
                }

                GeresEventSource.Log.JobHubSignalRServiceBusBridgeNotificationSentToSignalRHub(job.JobId, job.JobType, job.Status.ToString());
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.JobHugSignalRServiceBusBridgeNotificationHandleMessageFailed(ex.Message, ex.StackTrace);
            }
        }
    }
}
