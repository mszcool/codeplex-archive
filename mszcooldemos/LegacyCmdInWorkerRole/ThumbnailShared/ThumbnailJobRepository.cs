// <copyright file="ThumbnailJobRepository.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThumbnailShared
{
    public class ThumbnailJobRepository
    {
        private CloudTable _jobsTable;

        public ThumbnailJobRepository(string storageConnectionString)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var cloudTableClient = cloudStorageAccount.CreateCloudTableClient();
            _jobsTable = cloudTableClient.GetTableReference(SharedConstants.JobsTableName);
            _jobsTable.CreateIfNotExists();
        }

        public List<ThumbnailJobEntity> GetJobs()
        {
            var queryAllJobs = new TableQuery<ThumbnailJobEntity>().Where
                                (
                                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, ThumbnailJobEntity.JOBS_PARTITION_KEY)
                                );
            var results = _jobsTable.ExecuteQuery(queryAllJobs);

            return results.ToList();
        }

        public ThumbnailJobEntity GetJob(string jobId)
        {
            var queryAllJobs = new TableQuery<ThumbnailJobEntity>().Where
                                (
                                    TableQuery.CombineFilters(
                                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, ThumbnailJobEntity.JOBS_PARTITION_KEY),
                                        TableOperators.And,
                                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, jobId)
                                    )
                                );
            var results = _jobsTable.ExecuteQuery(queryAllJobs).FirstOrDefault();

            return results;
        }

        public string InsertNewJob(string sourceImageUrl, string targetImageName)
        {
            string newJobId = Guid.NewGuid().ToString();
            var newJob = new ThumbnailJobEntity(newJobId) 
            {
                SourceImageUrl = sourceImageUrl,
                TargetImageName = targetImageName,
                Status = ThumbnailJobEntity.JOB_STATUS_SCHEDULED,
                TargetImageUrl = string.Empty
            };
            var insertOperation = TableOperation.Insert(newJob);
            _jobsTable.Execute(insertOperation);
            return newJobId;
        }

        public void UpdateExistingJob(ThumbnailJobEntity existingEntity)
        {
            var updateOperation = TableOperation.Merge(existingEntity);
            _jobsTable.Execute(updateOperation);
        }
    }
}
