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
    /// Description of a job that gets scheduled and executed inside of the resource scheduler system.
    /// </summary>
    public class Job
    {
        /// <summary>
        /// Unique Identifier of the Job.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Friendly name of the Job specified by the caller.
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// Type of the Job that defines, which class/assembly needs to be instantiated for the implementation.
        /// </summary>
        public string JobType { get; set; }

        /// <summary>
        /// Parameters which are passed into the job, these need to be parsed by the
        /// actual job-implementation.
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Date and time when the job has been submitted for the first time.
        /// </summary>
        public DateTime SubmittedAt { get; set; }

        /// <summary>
        /// Name of the user who originally submitted the job to the system.
        /// </summary>
        public string ScheduledBy { get; set; }

        /// <summary>
        /// Reflects the current status of the job.
        /// </summary>
        public JobStatus Status { get; set; }

        /// <summary>
        /// Specifies the name of the ZIP-package to be downloaded from BLOB-storage
        /// </summary>
        public string JobProcessorPackageName { get; set; }

        /// <summary>
        /// Identification of the tenant to be used for the package deployment
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        /// Last output of the job
        /// </summary>
        public string Output { get; set; }
    }
}
