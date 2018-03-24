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
using Geres.AutoScaler.Interfaces.AutoScalerPolicy;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;

namespace GeresAutoscalerPolicySamples
{
    public class MessageCountAutoScalerPolicy : IAutoScalerPolicy
    {
        private readonly int _maxJobsPerInstance = 3;

        /// <summary>
        /// Constructor reads all the policies from the Azure configuration
        /// </summary>
        public MessageCountAutoScalerPolicy()
        {
            if (!RoleEnvironment.IsAvailable)
                throw new Exception("Unable to get access to the Windows Azure Role Environment!");

            try
            {
                MaximumJobHosts = int.Parse(CloudConfigurationManager.GetSetting(Constants.CONFIG_MAXIMUM_JOBHOSTS));
                MinimumRunningJobHosts = int.Parse(CloudConfigurationManager.GetSetting(Constants.CONFIG_MINIMUM_RUNNING_JOBHOSTS));
                MaximumIdleJobHosts = int.Parse(CloudConfigurationManager.GetSetting(Constants.CONFIG_MAXIMUM_IDLE_JOBHOSTS));
                IdleTime = TimeSpan.FromMinutes(int.Parse(CloudConfigurationManager.GetSetting(Constants.CONFIG_MAXIMUM_IDLE_TIME_IN_MIN)));
            }
            catch (Exception ex)
            {
                throw new Exception(
                    string.Format("Unable to read configuration setting values for AutoScaler Policy from Cloud Service Configuration: {0}!", ex.Message),
                    ex);
            }
        }

        /// <summary>
        /// Simple policy
        /// If the current instances need to process more than 100 jobs each then add a new instance per 100 jobs remainder
        /// e.g.1. 3 instances working with 700 jobs on the queue then add 4 new instances
        /// e.g.2. 3 instances working with 399 jobs on the queue then add 0 instances
        /// </summary>
        /// <param name="defaultQueue"></param>
        /// <param name="processorInstanceCount"></param>
        /// <returns>Number of instances to add.</returns>
        public int DoScaleOut(CloudQueue defaultQueue, IEnumerable<CloudQueue> batchQueues, int processorInstanceCount)
        {
            int delta = 0;
            int messageCount = 0;

            if (processorInstanceCount < this.MaximumJobHosts)
            {
                // fetch the queue attributes so that the number of jobs on the queue can be retrieved
                defaultQueue.FetchAttributes();

                int? count = defaultQueue.ApproximateMessageCount;

                if (count.HasValue)
                {
                    messageCount += count.Value;
                }

                // do the same for the batch queues
                foreach (var batchQueue in batchQueues)
                {
                    // fetch the queue attributes so that the number of jobs on the queue can be retrieved
                    batchQueue.FetchAttributes();

                    count = batchQueue.ApproximateMessageCount;

                    if (count.HasValue)
                    {
                        messageCount += count.Value;
                    }
                }

                if (messageCount > 0)
                {
                    var unallocated = messageCount - (processorInstanceCount * _maxJobsPerInstance);

                    if (unallocated >= _maxJobsPerInstance)
                    {
                        decimal division = unallocated / _maxJobsPerInstance;
                        int.TryParse(Math.Floor(division).ToString(), out delta);
                    }
                }
            }

            return delta;
        }

        public string PolicyType
        {
            get { return "MessageCount"; }
        }

        public int MaximumJobHosts
        {
            get;
            private set;
        }

        public TimeSpan IdleTime
        {
            get;
            private set;
        }

        public int MinimumRunningJobHosts
        {
            get;
            private set;
        }

        public int MaximumIdleJobHosts
        {
            get;
            private set;
        }
    }
}