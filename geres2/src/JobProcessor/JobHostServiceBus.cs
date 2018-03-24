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
using Geres.Common.Entities;
using Geres.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;

namespace Geres.Azure.PaaS.JobProcessor
{
    public class JobHostServiceBus
    {
        private readonly string _connectionString;
        private readonly string _commandsForAutoScalerTopicName;
        private readonly string _commandsForJobHostTopicName;
        private TopicClient _commandsForAutoScalerTopicClient;
        private TopicClient _commandsForJobHostTopicClient;
        private NamespaceManager _namespaceManager;

        public JobHostServiceBus(string connectionString, string commandsForAutoScalerTopicName, string commandsForJobHostTopicName)
        {
            _connectionString = connectionString;
            _commandsForAutoScalerTopicName = commandsForAutoScalerTopicName;
            _commandsForJobHostTopicName = commandsForJobHostTopicName;

            _commandsForJobHostTopicClient = TopicClient.CreateFromConnectionString(_connectionString,
                        _commandsForAutoScalerTopicName);
            _commandsForJobHostTopicClient.RetryPolicy = RetryPolicy.Default;
            
            _commandsForAutoScalerTopicClient = TopicClient.CreateFromConnectionString(_connectionString,
                        _commandsForAutoScalerTopicName);
            _commandsForAutoScalerTopicClient.RetryPolicy = RetryPolicy.Default;
        }

        private void InitializeTopics()
        {
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);
            _namespaceManager.Settings.RetryPolicy = RetryPolicy.Default;

            if (!_namespaceManager.TopicExists(_commandsForAutoScalerTopicName))
            {
                try
                {
                    var desc = new TopicDescription(_commandsForAutoScalerTopicName) { };
                    _namespaceManager.CreateTopic(desc);
                }
                catch (Microsoft.ServiceBus.Messaging.MessagingEntityAlreadyExistsException)
                {
                    // Another worker created the topic, already
                }
            }

            if (!_namespaceManager.TopicExists(_commandsForJobHostTopicName))
            {
                try
                {
                    var desc = new TopicDescription(_commandsForJobHostTopicName) { };
                    _namespaceManager.CreateTopic(desc);
                }
                catch (Microsoft.ServiceBus.Messaging.MessagingEntityAlreadyExistsException)
                {
                    // Another worker created the topic, already
                }
            }
        }

        public SubscriptionClient CreateSubscription(string subscriptionName, string roleInstanceId)
        {
            InitializeTopics();

            // If an existing subscription is there, delete it, so we can update the filter
            if (_namespaceManager.SubscriptionExists(_commandsForJobHostTopicName, subscriptionName))
            {
                _namespaceManager.DeleteSubscription(_commandsForJobHostTopicName, subscriptionName);
            }

            // Create the subscription again with the most up2date filter
            var desc = new SubscriptionDescription(_commandsForJobHostTopicName, subscriptionName);
            _namespaceManager.CreateSubscription
                (
                    desc,
                    new SqlFilter
                    (
                        string.Format(@"{0} = '{1}'", GlobalConstants.SERVICEBUS_MESSAGE_PROP_ROLEINSTANCEID, roleInstanceId)
                    )
                );

            // Subscribe with the subscription client
            var client = SubscriptionClient.CreateFromConnectionString(
                _connectionString,
                _commandsForJobHostTopicName,
                subscriptionName);

            client.RetryPolicy = RetryPolicy.Default;

            return client;
        }

        public void DeleteSubscription(string subscriptionName)
        {
            if (_namespaceManager.SubscriptionExists(_commandsForJobHostTopicName, subscriptionName))
            {
                _namespaceManager.DeleteSubscription(_commandsForJobHostTopicName, subscriptionName);
            }
        }

        public void SendAutoScaleUpdateMessage(JobHost jobHost)
        {
            InitializeTopics();

            var msg = new BrokeredMessage(jobHost)
            {
                //MessageId = jobHost.RoleInstanceId
            };

            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_ROLEINSTANCEID] = jobHost.RoleInstanceId;

            //var minBackoff = TimeSpan.FromSeconds(1);  // wait 5 minutes for the first attempt?
            //var maxBackoff = TimeSpan.FromSeconds(30);  // all attempts must be done within 15 mins?
            //var deltaBackoff = TimeSpan.FromSeconds(5); // the time between each attempt?
            //var terminationTimeBuffer = TimeSpan.FromSeconds(90); // the length of time each attempt is permitted to take?
            //var retryPolicy = new RetryExponential(minBackoff, maxBackoff, deltaBackoff, terminationTimeBuffer, 10);
            _commandsForAutoScalerTopicClient.Send(msg);
        }
    }
}
