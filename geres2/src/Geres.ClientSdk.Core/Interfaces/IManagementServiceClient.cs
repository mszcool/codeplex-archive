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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.ClientSdk.Core.Interfaces
{
    public interface IManagementServiceClient
    {
        Task<string> SubmitJob(Job newJob, string batchId = "");
        Task<List<string>> SubmitJobs(List<Job> newJobs, string batchId = "");
        Task CancelJob(string jobId);

        Task<string> CreateBatch(Batch newBatch);
        Task CloseBatch(string batchId);
        Task CancelBatch(string batchId);
    }
}
