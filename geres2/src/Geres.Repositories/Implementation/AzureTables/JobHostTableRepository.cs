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
using Geres.Repositories.Entities;
using Geres.Repositories.Interfaces;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Repositories.Implementation.AzureTables
{
    internal class JobHostTableRepository : IJobHostRepository
    {
        private const string JOBHOST_TABLENAME = "listofjobhosts";

        private CloudTable _azureTable;
        private string _deploymentId;

        internal JobHostTableRepository(string azureStorageConnectionString, string deploymentId)
        {
            CloudStorageAccount account = null;

            // Parameter Validations
            if (string.IsNullOrEmpty(azureStorageConnectionString))
                throw new ArgumentNullException("Parameter azureStorageConnectionString cannot be null for the BatchTableRepository!", "azureStorageConnectionString");
            if (!CloudStorageAccount.TryParse(azureStorageConnectionString, out account))
                throw new ArgumentException("Invalid storage connection string for your Azure storage account passed into repository creation!", "azureStorageConnectionString");

            // Next create and initialize the table object
            var tableClient = account.CreateCloudTableClient();
            _azureTable = tableClient.GetTableReference(JOBHOST_TABLENAME);
            _azureTable.CreateIfNotExists();

            // Each repository instance filters by a specific deploymentID so that different deployments
            // do not affect the AutoScaler behavior across those deployments (e.g. instances of old deployments
            // will get IDLE soon then the AutoScaler would attempt to remove those although not existing, anymore)
            if (string.IsNullOrEmpty(deploymentId))
                throw new ArgumentException("You have to specify a valid deployment for the JobHost repository since all workers need to belong to a specific deployment!");
            else
                _deploymentId = deploymentId;
        }

        public IQueryable<JobHostEntity> GetJobHosts()
        {
            // Compose a query by the partition key
            var tableQuery = CreateQuery(_deploymentId);

            // Execute the query
            var results = _azureTable.ExecuteQuery(tableQuery);

            // Return the query-results as IQueryable
            return results.AsQueryable<JobHostEntity>();
        }

        public JobHostEntity CreateJobHost(JobHostEntity entity)
        {
            // Parameter Validations
            ValidateEntityNull(entity);
            ValidateMandatoryParams(entity);

            // Every JobProcessor must be assigned to an appropriate deployment
            entity.DeploymentId = _deploymentId;

            // Next add the batch to the table
            var insertOp = TableOperation.Insert(entity);
            _azureTable.Execute(insertOp);

            // Return the entity with the updated ID
            return entity;
        }

        public void UpdateJobHost(JobHostEntity entity)
        {
            // Parameter Validation
            ValidateEntityNull(entity);
            ValidateMandatoryParams(entity);

            // If the entity belongs to a different deploymet, throw an exception
            if (string.Compare(entity.DeploymentId, _deploymentId, true) != 0)
                throw new InvalidOperationException("The entity you are trying to update must belong to the same deploymentId the repository has been created for!");

            // Update or replace the entity
            var updateOp = TableOperation.InsertOrMerge(entity);
            _azureTable.Execute(updateOp);
        }

        public void DeleteJobHost(string roleInstanceId)
        {
            // Parameter-validations
            if (string.IsNullOrEmpty(roleInstanceId))
                throw new ArgumentException("Parameter 'roleInstanceId' cannot be null or empty. Please specify the id of the JobHost to be deleted!", "batchId");

            // First of all try to find the existing batch
            var existingEntity = FindExistingJobHost(_deploymentId, roleInstanceId);

            // Now delete the entity
            var deleteOp = TableOperation.Delete(existingEntity);
            _azureTable.Execute(deleteOp);
        }

        public JobHostEntity GetJobHost(string roleInstanceId)
        {
            if (string.IsNullOrEmpty(roleInstanceId))
                throw new ArgumentException("Parameter 'roleInstanceId' cannot be null or empty. Please specify the id of the JobHost to be deleted!", "batchId");

            // First of all try to find the existing batch
            var existingEntity = FindExistingJobHost(_deploymentId, roleInstanceId);

            return existingEntity;
        }

        #region Private Helper Methods

        private static TableQuery<JobHostEntity> CreateQuery(string deploymentId)
        {
            var tableQuery = new TableQuery<JobHostEntity>().Where
                                                (
                                                    CreatePartitionKeyFilter(deploymentId)
                                                );
            return tableQuery;
        }

        public static TableQuery<JobHostEntity> CreateQuery(string deploymentId, string roleInstanceId)
        {
            var query = new TableQuery<JobHostEntity>().Where
                                                (
                                                    TableQuery.CombineFilters
                                                    (
                                                        CreatePartitionKeyFilter(deploymentId),
                                                        TableOperators.And,
                                                        CreateRowKeyFilter(roleInstanceId)
                                                    )
                                                );
            return query;
        }

        private static string CreatePartitionKeyFilter(string deploymentId)
        {
            return TableQuery.GenerateFilterCondition
                                (
                                    "PartitionKey",
                                    QueryComparisons.Equal,
                                    deploymentId
                                );
        }

        private static string CreateRowKeyFilter(string jobHostId)
        {
            return TableQuery.GenerateFilterCondition
                                (
                                    "RoleInstanceId",
                                    QueryComparisons.Equal,
                                    jobHostId
                                );
        }

        private static string CreateDeploymentIdFilter(string deploymentId)
        {
            return TableQuery.GenerateFilterCondition
                                (
                                    "DeploymentId",
                                    QueryComparisons.Equal,
                                    deploymentId
                                );
        }

        private JobHostEntity FindExistingJobHost(string deploymentId, string jobHostId)
        {
            var queryFindExisting = CreateQuery(deploymentId, jobHostId);
            var existingEntity = _azureTable.ExecuteQuery<JobHostEntity>(queryFindExisting).FirstOrDefault();
            return existingEntity;
        }

        #endregion

        #region Parameter Validation Helpers

        private static void ValidateMandatoryParams(JobHostEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Id)) throw new ArgumentException("Parameter 'entity.Id' cannot be null or empty for an update!");
        }

        private static void ValidateEntityNull(JobHostEntity entity)
        {
            if (entity == null) throw new ArgumentNullException("Parameter 'entity' cannot be null!", "entity");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            // Nothing to do
        }

        #endregion
    }
}
