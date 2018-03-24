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
    internal class RoleOperationStatusRepository : IRoleOperationStatusRepository
    {
        private const string ROLEOPERATIONSTATUS_TABLENAME = "roleoperationstatus";

        private CloudTable _azureTable;

        internal RoleOperationStatusRepository(string azureStorageConnectionString)
        {
            CloudStorageAccount account = null;

            // Parameter Validations
            if (string.IsNullOrEmpty(azureStorageConnectionString))
                throw new ArgumentNullException("Parameter azureStorageConnectionString cannot be null for the BatchTableRepository!", "azureStorageConnectionString");
            if (!CloudStorageAccount.TryParse(azureStorageConnectionString, out account))
                throw new ArgumentException("Invalid storage connection string for your Azure storage account passed into repository creation!", "azureStorageConnectionString");

            // Next create and initialize the table object
            var tableClient = account.CreateCloudTableClient();
            _azureTable = tableClient.GetTableReference(ROLEOPERATIONSTATUS_TABLENAME);
            _azureTable.CreateIfNotExists();
        }

        public Entities.RoleOperationStatusEntity CreateRoleOperationStatus(Entities.RoleOperationStatusEntity entity)
        {
            // Next add the batch to the table
            var insertOp = TableOperation.Insert(entity);
            _azureTable.Execute(insertOp);

            // Return the entity with the updated ID
            return entity;
        }

        public void UpdateRoleOperationStatus(Entities.RoleOperationStatusEntity entity)
        {
            // Now update the entity
            var updateOp = TableOperation.Merge(entity);
            _azureTable.Execute(updateOp);
        }

        public Entities.RoleOperationStatusEntity GetRoleOperationStatus()
        {
            // First of all try to find the existing batch
            var existingEntity = Find();

            return existingEntity;
        }


        #region Private Helper Methods

        public static TableQuery<RoleOperationStatusEntity> CreateQuery()
        {
            var query = new TableQuery<RoleOperationStatusEntity>().Where
                                                (
                                                    TableQuery.GenerateFilterCondition
                                                    (
                                                        "PartitionKey",
                                                        QueryComparisons.Equal,
                                                        RoleOperationStatusEntity.PARTITION_KEY
                                                    )
                                                );
            return query;
        }


        private RoleOperationStatusEntity Find()
        {
            var existingEntity = _azureTable.ExecuteQuery<RoleOperationStatusEntity>(CreateQuery()).FirstOrDefault();
            return existingEntity;
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

