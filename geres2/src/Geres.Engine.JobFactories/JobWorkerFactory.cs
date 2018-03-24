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
using Geres.Common.Interfaces.Engine;
using Geres.Common.Interfaces.Implementation;
using Geres.Engine.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Engine.JobFactories
{
    /// <summary>
    /// Creates a job implementation that runs in-process of the job host
    /// </summary>
    /// <remarks>
    /// With this approach we can encapsulate additional types of "standard-job-execution strategies"
    /// - One with the job worker process that loads .NET assemblies
    /// - Another one that might even enable calling java executables using java.exe
    /// - Calling "standard-EXE" applications deployed as part of the job package
    /// This first version just returns the Job Worker Process implementation for each type of job
    /// </remarks>
    public class JobWorkerFactory : IJobImplementationFactory
    {
        public IJobImplementation Lookup(Common.Entities.Job job)
        {
            return new JobWorkerProcessImplementation();    
        }
    }
}
