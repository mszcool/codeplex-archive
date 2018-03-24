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
using Geres.Common.Interfaces.Implementation;
using Geres.Common.Interfaces.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Engine
{
    public static class EngineFactory
    {
        public static IJobController CreateController()
        {
            var scheduler = new Geres.Engine.DefaultPaaS.JobController();
            scheduler.Initialize();
            return scheduler;
        }

        public static IJobHost CreateJobHandler(ITenantManager tenantManager)
        {
            var processor = new Geres.Engine.DefaultPaaS.JobHost();
            processor.Initialize(tenantManager);
            return processor;
        }

        public static ITenantManager CreateTenantManager(string baseDirectory, string baseTempDirectory)
        {
            var tenantManager = new Geres.Engine.DefaultPaaS.TenantManager();
            tenantManager.Initialize(baseDirectory, baseTempDirectory);
            return tenantManager;
        }
    }
}
