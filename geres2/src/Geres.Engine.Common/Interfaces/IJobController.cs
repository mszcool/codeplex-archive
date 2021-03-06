﻿//
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

namespace Geres.Common.Interfaces.Engine
{
    public interface IJobController
    {
        void Initialize();
        string SubmitJob(Job job, string batchId);
        List<string> SubmitJobs(List<Job> jobs, string batchId);
        void CancelJob(string jobId);
        Batch CreateBatch(Batch newBatch);
        void CancelBatch(string batchId);
        void CloseBatch(string batchId);
    }
}
