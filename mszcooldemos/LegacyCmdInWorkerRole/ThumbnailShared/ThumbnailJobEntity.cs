// <copyright file="ThumbnailJobEntity.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThumbnailShared
{
    public class ThumbnailJobEntity : TableEntity
    {
        public const string JOBS_PARTITION_KEY = "thumbnailJobs";
        public const string JOB_STATUS_SCHEDULED = "Scheduled";
        public const string JOB_STATUS_RUNNING = "Running";
        public const string JOB_STATUS_RETRY = "Retry after Failure";
        public const string JOB_STATUS_FAILED = "Failed after several retries";
        public const string JOB_STATUS_COMPLETED = "Completed";

        public ThumbnailJobEntity()
        {
        }

        public ThumbnailJobEntity(string jobId)
        {
            this.PartitionKey = JOBS_PARTITION_KEY;
            this.RowKey = jobId;
        }

        public string SourceImageUrl { get; set; }
        public string TargetImageName { get; set; }
        public string TargetImageUrl { get; set; }
        public string Status { get; set; }
    }
}
