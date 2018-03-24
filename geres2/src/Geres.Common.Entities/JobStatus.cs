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
    public enum JobStatus
    {
        Finished = 0,           // Note: keep this at 0 since it is used as an exit code from the worker process for jobs 
        Submitted = 10,
        Started = 11,
        InProgress = 12,
        Cancelled = 13,
        Failed = 50,
        FailedUnexpectedly = 51,
        Aborted = 60,
        AbortedTenantDeploymentFailed = 61,
        AbortedMaxRetryCount = 62,
        AbortedJobProcessorMissingOrFailedLoading = 63,
        AbortedInternalError = 64
    }
}
