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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Common.Entities
{
    /// <summary>
    /// Can be used to group jobs into batches for joint-opertions (e.g. tracking, deletion)
    /// </summary>
    public class Batch
    {
        /// <summary>
        /// Internal, unique identifier for the batch created
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// A unique identifier for a batch of jobs in the system
        /// </summary>
        public string BatchName { get; set; }

        /// <summary>
        /// Priority for the processing of the batch
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Status of the batch (whether it is open or closed)
        /// </summary>
        public BatchStatus Status { get; set; }

        /// <summary>
        /// Specifies whether dedicated workers will work on this batch or not
        /// </summary>
        public bool RequiresDedicatedWorker { get; set; }
    }
}