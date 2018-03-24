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
using Geres.Common;
using Geres.Common.Entities;
using Geres.Common.Interfaces.Implementation;
using System;
using System.Threading;

namespace Geres.Samples.SimpleJobs
{
    public class UpdateWaitingPostings : IJobImplementation
    {
        private const int NUMBEROFSTEPS = 10;

        // make the property volatile as it may be set by more than one thread
        private volatile bool _cancellationToken = false;

        // default constructor (required by MEF)
        public UpdateWaitingPostings()
        {
        }

        public JobProcessResult DoWork(Job job, string jobPackagePath, string jobWorkingPath, Action<string> progressCallback)
        {
            var jobStatus = JobStatus.Finished;
            var result = new JobProcessResult();


            // IsLongRunning infers no job progress is required other than its final state
            // ordinarily this will be inferred by the job type and not the client, but for the pupose of this PoC this will be hardcoded.

            for (int i = 1; i <= NUMBEROFSTEPS; i++)
            {
                if (_cancellationToken == true)
                {
                    jobStatus = JobStatus.Cancelled;
                    _cancellationToken = false;
                    break;
                }

                // do the job...
                // ...
                // ...
                Thread.Sleep(2500);

                progressCallback((i*10).ToString());
            }

            return new JobProcessResult { Status = jobStatus, Output = "okay" };
        }

        public string JobType
        {
            get { return "UpdateWaitingPostings"; }
        }

        public int NumberOfSteps
        {
            get
            {
                return NUMBEROFSTEPS;
            }
        }

        public void CancelProcessCallback()
        {
            _cancellationToken = true;
        }
    }
}