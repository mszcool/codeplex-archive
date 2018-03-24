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
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Repositories.Entities
{
    public class RoleOperationStatusEntity : TableEntity
    {
        internal const string PARTITION_KEY = "RoleOperationStatus";

        public RoleOperationStatusEntity()
        {
            this.PartitionKey = PARTITION_KEY;
            this.Id = Guid.NewGuid().ToString();
            this.RequestId = string.Empty;
        }

        [IgnoreProperty]                            // Ignoring the property because it matches to RowKey
        public string Id
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        public string RequestId { get; set; }
    }
}
