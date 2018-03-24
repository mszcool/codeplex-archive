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
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.AutoScaler.Interfaces.AutoScalerPolicy
{
    [InheritedExport]
    public interface IAutoScalerPolicy
    {
        /// <summary>
        /// Based on the queue and the attributes of it's job messages determine 
        /// how many new worker roles need to be added
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        int DoScaleOut(CloudQueue defaultQueue, IEnumerable<CloudQueue> batchQueues, int processorInstanceCount);

        /// <summary>
        /// Used to identify the policy as part of the MEF lookup  
        /// </summary>
        string PolicyType { get; }

        /// <summary>
        /// The minimum number of worker instances that need to be actively listening for a job? 
        /// </summary>
        int MinimumRunningJobHosts { get; }

        /// <summary>
        /// The maximum number of worker instances that can remain idle, i.e. they will not be removed
        /// from the cloud service but will wait until required.
        /// </summary>
        int MaximumIdleJobHosts { get; }

        /// <summary>
        /// The maximum number of worker instances that can be instantiated as part of this cloud service?
        /// </summary>
        int MaximumJobHosts { get; }

        /// <summary>
        /// How long can a JobHost remain idle before it is removed
        /// </summary>
        TimeSpan IdleTime { get; }
    }
}
