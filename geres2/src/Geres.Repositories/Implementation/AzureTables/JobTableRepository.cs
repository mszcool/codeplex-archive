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
using Geres.Util;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Repositories.Implementation.AzureTables
{
    internal class JobTableRepository : IJobsRepository
    {
        private const string JOB_TABLENAME = "listofjobs";
 
        private CloudTable _azureTable;

        public JobTableRepository(string azureStorageConnectionString)
        {
            CloudStorageAccount account = null;

            // Parameter Validations
            if (string.IsNullOrEmpty(azureStorageConnectionString))
                throw new ArgumentNullException("Parameter azureStorageConnectionString cannot be null for the JobTableRepository!", "azureStorageConnectionString");
            if (!CloudStorageAccount.TryParse(azureStorageConnectionString, out account))
                throw new ArgumentException("Invalid storage connection string for your Azure storage account passed into repository creation!", "azureStorageConnectionString");

            // Next create and initialize the table object
            var tableClient = account.CreateCloudTableClient();
            _azureTable = tableClient.GetTableReference(JOB_TABLENAME);
            _azureTable.CreateIfNotExists();
        }

        public JobEntity GetJob(string jobId)
        {
            return GetJob(jobId, GlobalConstants.DEFAULT_BATCH_ID);
        }

        public JobEntity GetJob(string jobId, string batchId)
        {
            // Validate the parameters
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentException("JobId needs to be provided to get a single job.", "jobId");
            if (string.IsNullOrEmpty(batchId))
                throw new ArgumentException("BatchId needs to be provided to get a single job.", "batchId");

            // Find the job
            try
            {
                var existingJob = FindExistingJob(jobId, batchId);
                return existingJob;
            }
            catch (ApplicationException)
            {
                return null;
            }
        }

        public IQueryable<Entities.JobEntity> GetJobs(int numOfRecordsToReturn, ref string continuationToken)
        {
            var batchId = GlobalConstants.DEFAULT_BATCH_ID;
            return GetJobs(batchId, numOfRecordsToReturn, ref continuationToken);
        }

        public IQueryable<Entities.JobEntity> GetJobs(string batchId, int numOfRecordsToReturn, ref string continuationToken)
        {
            var query = CreateQuery(batchId).Take(numOfRecordsToReturn);
            var response = ExecuteSegmentedQuery(ref continuationToken, query);
            return response.AsQueryable();
        }

        public Entities.JobEntity CreateJob(Entities.JobEntity newJob)
        {
            // First of all validate parameters
            ValidateEntityNull(newJob, "newJob");
            ValidateMandatoryParams(newJob);

            // Then create the method for insertions
            var insertOp = TableOperation.Insert(newJob);
            _azureTable.Execute(insertOp);

            // Return the entity
            return newJob;
        }

        public void UpdateJob(Entities.JobEntity job)
        {
            // Parameter Validations
            ValidateEntityNull(job, "job");
            ValidateMandatoryParams(job);

            // Next find the existing entity
            FindExistingJob(job.JobId, job.BatchId);

            // Now Update the job
            var updateOp = TableOperation.Merge(job);
            _azureTable.Execute(updateOp);
        }

        public void DeleteJob(string jobId, string batchId)
        {
            // Parameter validations
            if (string.IsNullOrEmpty(jobId))
                throw new ArgumentException("You have to specify a jobId for deleting a job!", "jobId");
            if (string.IsNullOrEmpty(batchId))
                throw new ArgumentException("You have to specify a batchId for deleting a job!", "batchId");

            // Try to find the existing entity
            var existingJob = FindExistingJob(jobId, batchId);

            // Delete the entity
            var deleteOp = TableOperation.Delete(existingJob);
            _azureTable.Execute(deleteOp);
        }

        #region Private Helper Methods

        private static TableQuery<JobEntity> CreateQuery(string batchId)
        {
            var tableQuery = new TableQuery<JobEntity>().Where
                                                (
                                                    CreatePartitionKeyFilter(batchId)
                                                );
            return tableQuery;
        }

        public static TableQuery<JobEntity> CreateQuery(string jobId, string batchId)
        {
            var query = new TableQuery<JobEntity>().Where
                                                (
                                                    TableQuery.CombineFilters
                                                    (
                                                        CreatePartitionKeyFilter(batchId),
                                                        TableOperators.And,
                                                        CreateRowKeyFilter(jobId)
                                                    )
                                                );
            return query;
        }

        private static string CreatePartitionKeyFilter(string batchId)
        {
            return TableQuery.GenerateFilterCondition
                                (
                                    "PartitionKey",
                                    QueryComparisons.Equal,
                                    batchId
                                );
        }

        private static string CreateRowKeyFilter(string jobId)
        {
            return TableQuery.GenerateFilterCondition
                                (
                                    "RowKey",
                                    QueryComparisons.Equal,
                                    jobId
                                );
        }

        private JobEntity FindExistingJob(string jobId, string batchId)
        {
            var queryFindExisting = CreateQuery(jobId, batchId);
            var existingEntity = _azureTable.ExecuteQuery<JobEntity>(queryFindExisting).FirstOrDefault();
            
            return existingEntity;
        }

        private TableQuerySegment<JobEntity> ExecuteSegmentedQuery(ref string continuationToken, TableQuery<JobEntity> query)
        {
            TableContinuationToken continueToken = null;
            if (!string.IsNullOrEmpty(continuationToken))
            {
                var tokenParts = continuationToken.Split('|');
                if (tokenParts.Length != 2)
                    throw new ArgumentException("Invalid continuation token passed into JobRepository: " + continueToken);

                continueToken = new TableContinuationToken();
                continueToken.NextPartitionKey = tokenParts[0];
                continueToken.NextRowKey = tokenParts[1];
            }
            var response = _azureTable.ExecuteQuerySegmented<JobEntity>(query, continueToken);
            if (response.ContinuationToken != null)
            {
                continuationToken = string.Join("|", response.ContinuationToken.NextPartitionKey, response.ContinuationToken.NextRowKey);
            }
            else
            {
                continuationToken = null;
            }
            return response;
        }

        #endregion

        #region Parameter Validation Helpers

        private static void ValidateMandatoryParams(Entities.JobEntity entity)
        {
            if (string.IsNullOrEmpty(entity.JobId)) throw new ArgumentException("JobId must be passed into the repository for all operations!", "entity.JobId");
            if (string.IsNullOrEmpty(entity.Name)) throw new ArgumentException("Job must have a property 'Name' assigned with a value!", "entity.Name");
            if (entity.Parameters == null) throw new ArgumentNullException("Parameters cannot be null, please send string.empty if you don't want to pass parameters!", "entity.Parameters");
            if (entity.ScheduledBy == null) throw new ArgumentNullException("ScheduledBy cannot be null, please send string.empty if you don't want to specify a user name!", "entity.ScheduledBy");
            if (string.IsNullOrEmpty(entity.Type)) throw new ArgumentException("Job-type must be specified in 'Type' property!", "entity.Type");
            if (string.IsNullOrEmpty(entity.BatchId)) throw new ArgumentException("BatchId is missing in Job and is mandatory!", "entity.BatchId");
            if (string.IsNullOrEmpty(entity.BatchName)) throw new ArgumentException("BatchName is missing in Job and is mandatory!", "entity.BatchName");
        }

        private static void ValidateEntityNull(Entities.JobEntity entity, string paramName)
        {
            if (entity == null)
                throw new ArgumentNullException("You need to pass in a JobEntity into the repository, parameter 'entity' cannot be null!", paramName);
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
