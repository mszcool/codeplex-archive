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
using Geres.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Geres.Engine.Util
{
    public class JobNotificationServiceBus
    {
        private readonly string _connectionString;
        private readonly string _notificationTopicName;
        private TopicClient _topicClient;

        public JobNotificationServiceBus(string connectionString, string notificationTopicName)
        {
            _connectionString = connectionString;
            _notificationTopicName = notificationTopicName;
        }

        private void InitializeTopic()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);

            if (!namespaceManager.TopicExists(_notificationTopicName))
            {
                var desc = new TopicDescription(_notificationTopicName) { };
                namespaceManager.CreateTopic(desc);
            }

            _topicClient = TopicClient.CreateFromConnectionString(_connectionString,
                _notificationTopicName);

            _topicClient.RetryPolicy = RetryPolicy.Default;
        }

        public void SendStartedMessage(Job job)
        {
            InitializeTopic();
            var msg = new BrokeredMessage(job);

            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBID] = job.JobId;
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBNAME] = job.JobName;
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS] = JobStatus.Started.ToString();

            SendMessage(msg);
        }

        public void SendProgressMessage(Job job, string progress)
        {
            InitializeTopic();

            var msg = new BrokeredMessage(job);

            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBID] = job.JobId;
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBNAME] = job.JobName;
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS] = JobStatus.InProgress.ToString();

            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_PROGRESSUNIT] = progress;

            SendMessage(msg);
        }

        public void SendFinishedMessage(Job job, JobStatus status, string jobOutput)
        {
            if (string.IsNullOrEmpty(job.JobId))
                return;

            InitializeTopic();
            var msg = new BrokeredMessage(job);

            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBID] = job.JobId;
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBNAME] = job.JobName;
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBSTATUS] = status.ToString();
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBOUTPUT] = jobOutput;

            SendMessage(msg);
        }

        private void SendMessage(BrokeredMessage msg)
        {
            //var minBackoff = TimeSpan.FromSeconds(1);  // wait 5 minutes for the first attempt?
            //var maxBackoff = TimeSpan.FromSeconds(30);  // all attempts must be done within 15 mins?
            //var deltaBackoff = TimeSpan.FromSeconds(5); // the time between each attempt?
            //var terminationTimeBuffer = TimeSpan.FromSeconds(90); // the length of time each attempt is permitted to take?
            //var retryPolicy = new RetryExponential(minBackoff, maxBackoff, deltaBackoff, terminationTimeBuffer, 10);
            _topicClient.Send(msg);
        }
    }
}
