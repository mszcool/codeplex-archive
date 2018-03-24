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
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Repositories.Entities
{
    public class JobHostEntity : TableEntity
    {
        public JobHostEntity()
        {
            this.PartitionKey = string.Empty;           // PartitionKey is supposed to be the deploymentId
            this.Status = JobHostStatus.Preparing;
            this.DedicatedBatchId = string.Empty;
            this.Id = Guid.NewGuid().ToString();
            this.RoleInstanceId = Guid.NewGuid().ToString();
        }

        public JobHostEntity(JobHost jobHost)
        {
            this.PartitionKey = jobHost.DeploymentId;
            this.Status = jobHost.Status;

            // this will be the name of the role instance
            this.RoleInstanceId = jobHost.RoleInstanceId;
            this.DedicatedBatchId = jobHost.DedicatedBatchId;
            this.Id = jobHost.Id;
        }

        /// <summary>
        /// The ID of the deployment to which the JobProcessor is deployed
        /// </summary>
        [IgnoreProperty]                            // Ignoring since this is stored as partition key
        public string DeploymentId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        /// <summary>
        /// Unique Identifier for the JobProcessor
        /// </summary>
        [IgnoreProperty]                            // Ignoring the property because it matches to RowKey
        public string Id
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        /// <summary>
        /// Status of the Job Processor as Enumeration
        /// </summary>
        [IgnoreProperty]
        public JobHostStatus Status { get; set; }

        /// <summary>
        /// This is the <code>Status</code> property converted into <code>string</code> to make it serializable for table storage.
        /// </summary>
        public string StatusAsString
        {
            get { return this.Status.ToString(); }
            set { this.Status = (JobHostStatus)Enum.Parse(typeof(JobHostStatus), value); }
        }

        /// <summary>
        /// Used to notify a JobHost that it must work on a specific queue until told otherwise
        /// </summary>
        public string DedicatedBatchId { get; set; }

        /// <summary>
        /// The name of the JobProcessor worker role in the deployment
        /// </summary>
        public string RoleInstanceId { get; set; }
    }
}
