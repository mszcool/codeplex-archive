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

namespace Geres.Engine.Jobs
{
    public static class JobWorkerProcessConstants
    {
        // Job-specific constants
        public const string JOB_TYPE_NAME = "Geres.Engine.Jobs.GeresWorkerProcess";
        public const string JOB_XML_FILE = "job.xml";
        public const string JOB_WORKERPROC_EXENAME = "geresjobwp.exe";

        // Communication-specific constants for job progress reporting through stdout and stderr
        // To keep the process as simple as possible, we wanted to avoid creating other inter-process communication mechanisms (e.g. WCF Named Pipes)
        // Note: the exe is added as a reference, therefore it's in the WorkerRoles bin-directory
        public const string JOB_STDOUT_PROGRESS_REPORT = "Progress:";

        // When the job gets cancelled, the job implementation tries to send a graceful shutdown message. If that is not processed
        // within the timeframe specified below, the process is killed
        public const int JOB_DEFAULT_CANCELLATION_TIMEOUT_IN_MILLISECONDS = 20000;

        // Environment variables are used to provide "context-information" to the worker process
        public const string JOB_ENVIRONMENT_EXEC_PATH = "geresjobexecutionpath";
        public const string JOB_ENVIRONMENT_WORK_PATH = "geresjobworkingpath";
    }
}
