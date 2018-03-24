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
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Repositories.Entities
{
    public class JobEntity : TableEntity
    {
        public JobEntity()
        {
            this.BatchId = GlobalConstants.DEFAULT_BATCH_ID;    // Wraps the PartitionKey
        }

        public JobEntity(string batchId, string batchName)
        {
            InitializePropsWithDefaults();
            this.BatchId = batchId;
            this.BatchName = BatchName;
        }

        public JobEntity(Job job)
        {
            InitializePropsWithDefaults();
            InitilizePropsFromJob(job);
        }

        public JobEntity(Job job, string batchId, string batchName)
        {
            InitializePropsWithDefaults();
            InitilizePropsFromJob(job);
            this.BatchId = batchId;
            this.BatchName = BatchName;
        }

        private void InitializeOtherProps()
        {
            throw new NotImplementedException();
        }

        [IgnoreProperty]
        // Ignoring for storage since it wraps the PartitionKey
        public string BatchId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        [IgnoreProperty]
        // Ignoring for storage since it wraps the RowKey
        public string JobId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public string Name { get; set; }
        public string BatchName { get; set; }
        public string Type { get; set; }
        public string Parameters { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string ScheduledBy { get; set; }
        public string JobProcessorPackageName { get; set; }
        public string TenantName { get; set; }
        public string JobOutput { get; set; }
        public string JobOutputSource { get; set; }
        public string JobProcessorLastExecutingInstance { get; set; }

        [IgnoreProperty]
        public JobStatus Status { get; set; }

        /// <summary>
        /// This is the <code>Status</code> property converted into <code>string</code> to make it serializable for table storage.
        /// </summary>
        public string StatusAsString
        {
            get { return this.Status.ToString(); }
            set { this.Status = (JobStatus)Enum.Parse(typeof(JobStatus), value); }
        }

        #region Helper Methods

        private void InitializePropsWithDefaults()
        {
            this.BatchId = GlobalConstants.DEFAULT_BATCH_ID;    // Wraps the PartitionKey
            this.JobId = Guid.NewGuid().ToString();             // Wraps the RowKey
            this.Name = string.Empty;
            this.Type = string.Empty;
            this.Parameters = string.Empty;
            this.ScheduledBy = string.Empty;
            this.SubmittedAt = DateTime.UtcNow;
            this.Status = JobStatus.Submitted;
            this.JobOutput = string.Empty;
        }

        private void InitilizePropsFromJob(Job job)
        {
            this.JobId = job.JobId;
            this.Name = job.JobName;
            this.Type = job.JobType;
            this.Parameters = job.Parameters;
            this.ScheduledBy = job.ScheduledBy;
            this.SubmittedAt = job.SubmittedAt;
            this.JobProcessorPackageName = job.JobProcessorPackageName;
            this.TenantName = job.TenantName;
            this.JobOutput = job.Output;
        }

        public static Job ConvertToJob(JobEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            return new Job()
            {
                JobId = entity.JobId,
                JobType = entity.Type,
                JobName = entity.Name,
                Parameters = entity.Parameters,
                ScheduledBy = entity.ScheduledBy,
                SubmittedAt = entity.SubmittedAt,
                Status = entity.Status,
                JobProcessorPackageName = entity.JobProcessorPackageName,
                TenantName = entity.TenantName,
                Output = entity.JobOutput
            };
        }

        #endregion
    }
}
