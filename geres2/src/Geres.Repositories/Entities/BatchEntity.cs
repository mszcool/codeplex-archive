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
    public class BatchEntity : TableEntity
    {
        internal const string PARTITION_KEY = "BatchEntries";

        public BatchEntity()
        {
            this.PartitionKey = PARTITION_KEY;
            this.Id = Guid.NewGuid().ToString();    // Id wraps the RowKey
        }

        public BatchEntity(string batchName)
        {
            this.PartitionKey = PARTITION_KEY;
            this.Id = batchName;                    // Id wraps the RowKey
        }

        public BatchEntity(Batch batch)
        {
            this.PartitionKey = PARTITION_KEY;
            this.Id = batch.Id;                     // Id wraps the RowKey
            this.Name = batch.BatchName;
            this.Priority = batch.Priority;
            this.Created = DateTime.UtcNow;
            this.Status = batch.Status;
            this.RequiresDedicatedWorker = batch.RequiresDedicatedWorker;
        }

        [IgnoreProperty]                            // Ignoring the property because it matches to RowKey
        public string Id 
        {
            get { return this.RowKey; }
            set { this.RowKey = value; } 
        }

        public string Name { get; set; }
        public int Priority { get; set; }
        public DateTime Created { get; set; }
        
        [IgnoreProperty]
        public BatchStatus Status { get; set; }

        /// <summary>
        /// This is the <code>Status</code> property converted into <code>string</code> to make it serializable for table storage.
        /// </summary>
        public string StatusAsString
        {
            get { return this.Status.ToString(); }
            set { this.Status = (BatchStatus)Enum.Parse(typeof(BatchStatus), value); }
        }


        public bool RequiresDedicatedWorker { get; set; }
    }
}
