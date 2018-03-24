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
using Geres.Repositories.Implementation.AzureTables;
using Geres.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Repositories
{
    public static class RepositoryFactory
    {
        public static IJobsRepository CreateJobsRepository(string connectionString)
        {
            return new JobTableRepository(connectionString);
        }

        public static IBatchRepository CreateBatchRepository(string connectionString)
        {
            return new BatchTableRepository(connectionString);
        }

        public static IJobHostRepository CreateJobHostRepository(string connectionString, string deploymentId)
        {
            return new JobHostTableRepository(connectionString, deploymentId);
        }

        public static IRoleOperationStatusRepository CreateRoleOperationStatusRepository(string connectionString)
        {
            return new RoleOperationStatusRepository(connectionString);
        }
    }
}
