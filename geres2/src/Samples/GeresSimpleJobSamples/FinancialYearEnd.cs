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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Geres.Samples.SimpleJobs
{
    public class FinancialYearEnd : IJobImplementation
    {
        // make the property volatile as it may be set by more than one thread
        private volatile bool _cancellationToken = false;


        // default constructor (required by MEF)
        public FinancialYearEnd()
        {
        }

        public JobProcessResult DoWork(Job job,
            string jobPackagePath, string jobWorkingPath, 
            Action<string> progressCallback)
        {
            var status = JobStatus.Finished;
            var output = string.Empty;

            // cancellation callback method can set this property
            if (_cancellationToken)
                status = JobStatus.Cancelled;
            else
            {
                Thread.Sleep(10000);
                output = "okay";
            }

            return new JobProcessResult { Status = status, Output = string.Empty };
        }

        public string JobType
        {
            get { return "FinancialYearEnd"; }
        }


        public void CancelProcessCallback()
        {
            _cancellationToken = true;
        }
    }
}
