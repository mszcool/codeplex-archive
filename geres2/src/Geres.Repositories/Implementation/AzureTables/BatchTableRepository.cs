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
    internal class BatchTableRepository : IBatchRepository
    {
        private const string BATCH_TABLENAME = "listofbatches";

        private CloudTable _azureTable;

        internal BatchTableRepository(string azureStorageConnectionString)
        {
            CloudStorageAccount account = null;

            // Parameter Validations
            if (string.IsNullOrEmpty(azureStorageConnectionString)) 
                throw new ArgumentNullException("Parameter azureStorageConnectionString cannot be null for the BatchTableRepository!", "azureStorageConnectionString");
            if(!CloudStorageAccount.TryParse(azureStorageConnectionString, out account))
                throw new ArgumentException("Invalid storage connection string for your Azure storage account passed into repository creation!", "azureStorageConnectionString");

            // Next create and initialize the table object
            var tableClient = account.CreateCloudTableClient();
            _azureTable = tableClient.GetTableReference(BATCH_TABLENAME);
            _azureTable.CreateIfNotExists();
        }

        public IQueryable<Entities.BatchEntity> GetBatches()
        {
            // Compose a query by the partition key
            var tableQuery = CreateQuery();

            // Execute the query
            var results = _azureTable.ExecuteQuery(tableQuery);

            // Return the query-results as IQueryable
            return results.AsQueryable<BatchEntity>();
        }

        public Entities.BatchEntity CreateBatch(Entities.BatchEntity entity)
        {
            // Parameter Validations
            ValidateEntityNull(entity);
            ValidateMandatoryParams(entity);

            // Next add the batch to the table
            var insertOp = TableOperation.Insert(entity);
            _azureTable.Execute(insertOp);

            // Return the entity with the updated ID
            return entity;
        }

        public void UpdateBatch(Entities.BatchEntity entity)
        {
            // Parameter Validation
            ValidateEntityNull(entity);
            ValidateMandatoryParams(entity);

            // First of all try to find the existing batch
            var existingEntity = FindExistingBatch(entity.Id);

            // Now update the entity
            var updateOp = TableOperation.Merge(entity);
            _azureTable.Execute(updateOp);
        }

        public void DeleteBatchEntity(string batchId)
        {
            // Parameter-validations
            if (string.IsNullOrEmpty(batchId)) 
                throw new ArgumentException("Parameter 'batchId' cannot be null or empty. Please specify the id of the batch to be deleted!", "batchId");

            // First of all try to find the existing batch
            var existingEntity = FindExistingBatch(batchId);

            // Now delete the entity
            var deleteOp = TableOperation.Delete(existingEntity);
            _azureTable.Execute(deleteOp);
        }

        public BatchEntity GetBatch(string batchId)
        {
            if (string.IsNullOrEmpty(batchId))
                throw new ArgumentException("Parameter 'batchId' cannot be null or empty. Please specify the id of the batch to be deleted!", "batchId");

            // First of all try to find the existing batch
            var existingEntity = FindExistingBatch(batchId);
            
            return existingEntity;
        }

        #region Private Helper Methods

        private static TableQuery<BatchEntity> CreateQuery()
        {
            var tableQuery = new TableQuery<BatchEntity>().Where
                                                (
                                                    CreatePartitionKeyFilter()
                                                );
            return tableQuery;
        }

        public static TableQuery<BatchEntity> CreateQuery(string batchId)
        {
            var query = new TableQuery<BatchEntity>().Where
                                                (
                                                    TableQuery.CombineFilters
                                                    (
                                                        CreatePartitionKeyFilter(),
                                                        TableOperators.And,
                                                        CreateRowKeyFilter(batchId)
                                                    )
                                                );
            return query;
        }

        private static string CreatePartitionKeyFilter()
        {
            return TableQuery.GenerateFilterCondition
                                (
                                    "PartitionKey",
                                    QueryComparisons.Equal,
                                    BatchEntity.PARTITION_KEY
                                );
        }

        private static string CreateRowKeyFilter(string batchId)
        {
            return TableQuery.GenerateFilterCondition
                                (
                                    "RowKey",
                                    QueryComparisons.Equal,
                                    batchId
                                );
        }

        private BatchEntity FindExistingBatch(string batchId)
        {
            var queryFindExisting = CreateQuery(batchId);
            var existingEntity = _azureTable.ExecuteQuery<BatchEntity>(queryFindExisting).FirstOrDefault();
            if (existingEntity == null)
                throw new Exception(string.Format("Existing batch with id {0} does not exist for being updated!", batchId));
            return existingEntity;
        }

        #endregion

        #region Parameter Validation Helpers

        private static void ValidateMandatoryParams(Entities.BatchEntity entity)
        {
            if (string.IsNullOrEmpty(entity.Id)) throw new ArgumentException("Parameter 'entity.Id' cannot be null or empty for an update!");
            if (string.IsNullOrEmpty(entity.Name)) throw new ArgumentException("Parameter 'entity.Name' cannot be null or empty!");
            if (entity.Priority < 0) throw new ArgumentException("Parameter 'entity.Priority' must be >= 0!");
        }

        private static void ValidateEntityNull(Entities.BatchEntity entity)
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
