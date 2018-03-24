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
using Geres.Util;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;

namespace Geres.Engine.Util
{
    public class JobCancellationServiceBus
    {
        private readonly string _connectionString;
        private readonly string _cancellationTopicName;
        private TopicClient _topicClient;
        private NamespaceManager _namespaceManager;
        private TimeSpan _cancellationTimeWindow;
        private TimeSpan _cancellationMessageTimeToLive;

        public JobCancellationServiceBus(string connectionString, string cancellationTopicName, TimeSpan cancellationTimeWindow, TimeSpan cancellationMessageTimeToLive)
        {
            _connectionString = connectionString;
            _cancellationTopicName = cancellationTopicName;
            _cancellationMessageTimeToLive = cancellationMessageTimeToLive;
            _cancellationTimeWindow = cancellationTimeWindow;
        }

        private void InitializeTopic()
        {
            _namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);

            try
            {

                if (!_namespaceManager.TopicExists(_cancellationTopicName))
                {
                    var desc = new TopicDescription(_cancellationTopicName) 
                    {
                        DefaultMessageTimeToLive = _cancellationMessageTimeToLive
                    };
                    _namespaceManager.CreateTopic(desc);
                }
            }
            catch { }

            _topicClient = TopicClient.CreateFromConnectionString(_connectionString,
                _cancellationTopicName);
        }

        public SubscriptionClient CreateSubscription(string jobId, string subscriptionName)
        {
            InitializeTopic();

            if (_namespaceManager.SubscriptionExists(_cancellationTopicName, subscriptionName) == false)
            {
                var desc = new SubscriptionDescription(_cancellationTopicName, subscriptionName)
                {
                    AutoDeleteOnIdle = _cancellationTimeWindow
                };

                _namespaceManager.CreateSubscription
                    (
                        desc,
                        new SqlFilter
                        (
                            string.Format(@"{0} = '{1}'", GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBID, jobId)
                        )
                    );
            }

            var client = SubscriptionClient.CreateFromConnectionString(
                _connectionString,
                _cancellationTopicName,
                subscriptionName);

            client.RetryPolicy = RetryPolicy.Default;

            return client;
        }

        public void DeleteSubscription(string subscriptionName)
        {
            if (_namespaceManager.SubscriptionExists(_cancellationTopicName, subscriptionName))
            {
                _namespaceManager.DeleteSubscription(_cancellationTopicName, subscriptionName);
            }
        }

        public static bool CancellationTopicExists(string connectionString, string topicName)
        {
            var nsManager = NamespaceManager.CreateFromConnectionString(connectionString);
            return nsManager.TopicExists(topicName);
        }

        public static void SendCancellationMessage(string jobId, string connectionString, string topicName)
        {
            // Senders should not create the topic to remain settings with which the topic is created, consistent
            // --old -- InitializeTopic();
            var topicClient = TopicClient.CreateFromConnectionString(connectionString, topicName);

            // Create and send the message
            var msg = new BrokeredMessage();
            msg.Properties[GlobalConstants.SERVICEBUS_MESSAGE_PROP_JOBID] = jobId;

            // Send the message to the (existing) topic
            topicClient.RetryPolicy = RetryPolicy.Default;
            topicClient.Send(msg);
        }
    }
}
