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
    public class MessageWaitingTimeAutoScalerPolicy : IAutoScalerPolicy
    {
        /// <summary>
        /// Constructor reads all the policies from the Azure configuration
        /// </summary>
        public MessageWaitingTimeAutoScalerPolicy()
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
        /// If the first message has been waiting for longer than 5 minutes then add a new role.
        /// </summary>
        /// <param name="defaultQueue"></param>
        /// <param name="processorInstanceCount"></param>
        /// <returns>Number of instances to add.</returns>
        public int DoScaleOut(CloudQueue defaultQueue, IEnumerable<CloudQueue> batchQueues,  int processorInstanceCount)
        {
            int delta = 0;

            // maximum number of worker instances working is 20 so don't increase it by anymore
            if (processorInstanceCount < 20)
            {
                int? count = defaultQueue.ApproximateMessageCount;

                if (count.HasValue)
                {
                    if (count.Value > 0)
                    {
                        var msg = defaultQueue.PeekMessage();

                        DateTimeOffset? insertionTime = msg.InsertionTime;
 
                        if (insertionTime.HasValue)
                        {
                            if (DateTimeOffset.Now - insertionTime > TimeSpan.FromMinutes(5))
                            {
                                delta = 1;
                            }
                        }
                    }
                }
            }

            return delta;
        }

        public string PolicyType
        {
            get { return "MessageWaitingTime"; }
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