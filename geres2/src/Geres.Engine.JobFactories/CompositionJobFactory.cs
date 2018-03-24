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
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using Geres.Common.Entities;
using Geres.Common.Interfaces.Implementation;
using Geres.Common.Interfaces.Engine;

namespace Geres.Engine.JobFactories
{
    public class CompositionJobFactory : IJobImplementationFactory
    {
        private IEnumerable<Lazy<IJobImplementation>> _jobs;

        public CompositionJobFactory(string folder)
        {
            var catalog = new DirectoryCatalog(folder, "*.dll");

            var container = new CompositionContainer(catalog);
            _jobs = container.GetExports<IJobImplementation>();
        }

        public IJobImplementation Lookup(Job job)
        {
            var lazy = _jobs.SingleOrDefault(p => p.Value.JobType == job.JobType);
            if (lazy != null)
            {
                var jobImpl = lazy.Value;

                return jobImpl;
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
    }
}
