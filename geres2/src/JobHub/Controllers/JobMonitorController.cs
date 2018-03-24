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
using Geres.Common.Entities;
using Geres.Diagnostics;
using Geres.Repositories;
using Geres.Repositories.Entities;
using Geres.Util;
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace Geres.Azure.PaaS.JobHub.Controllers
{
    [Authorize]
    [RoutePrefix("api/jobmonitor")]
    public class JobMonitorController : ApiController
    {
        [HttpGet]
        [Route("job/{jobId}")]
        public Job GetJobStatus(string jobId, string batchId = "")
        {
            try
            {
                // Parameter Assertions
                GeresAssertionHelper.AssertNullOrEmpty(jobId, "jobId");

                // Logging the message
                GeresEventSource.Log.WebApiMonitoringQueryJobReceived(jobId);

                // Core execution logic
                using (var repo = RepositoryFactory.CreateJobsRepository(CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME)))
                {
                    JobEntity jobEntity = null;
                    if (!string.IsNullOrEmpty(batchId))
                        jobEntity = repo.GetJob(jobId, batchId);
                    else
                        jobEntity = repo.GetJob(jobId);

                    if (jobEntity == null)
                        throw new HttpResponseException(HttpStatusCode.NotFound);

                    Job job = JobEntity.ConvertToJob(jobEntity);

                    GeresEventSource.Log.WebApiMonitoringQueryJobSuccessful(job.JobId, job.JobType);

                    return job;
                }
            }
            catch (HttpResponseException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobMonitorController -- Invalid parameter passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiJobMonitoringInvalidParameterReceived(jobId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobMonitorController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [Route("batch/{batchId}")]
        [Queryable]
        public IQueryable<Job> GetBatchStatus(string batchId, int numOfRecords = 1000, string continuationToken = "")
        {
            try
            {
                // Parameter Validations
                GeresAssertionHelper.AssertNullOrEmpty(batchId, "batchId");

                // Log the query request
                GeresEventSource.Log.WebApiMonitoringQueryBatchReceived(batchId);

                using (var repo = RepositoryFactory.CreateJobsRepository(CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME)))
                {
                    var jobEntities = repo.GetJobs(batchId, numOfRecords, ref continuationToken);
                    if (jobEntities == null)
                    {
                        throw new HttpResponseException(HttpStatusCode.NotFound);
                    }

                    List<Job> jobs = new List<Job>();
                    foreach (JobEntity je in jobEntities)
                    {
                        Job job = JobEntity.ConvertToJob(je);
                        jobs.Add(job);
                    }

                    // If the continuation token is not null, add it to the HTTP Header
                    if (!string.IsNullOrEmpty(continuationToken))
                    {
                        System.Web.HttpContext.Current.Response.AddHeader("gerespagingtoken", continuationToken);
                    }

                    // Log the query completion
                    GeresEventSource.Log.WebApiMonitoringQueryBatchSuccessful(batchId, jobs.Count);

                    return jobs.AsQueryable();
                }
            }
            catch (HttpResponseException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobMonitorController -- Invalid parameter passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiBatchMonitoringInvalidParameterReceived(batchId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobMonitorController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        [Route("batch")]
        [Queryable]
        public IQueryable<Batch> GetBatches()
        {
            try
            {
                string getAllIdentifierForLogs = "--get all--";

                // Log the query request
                GeresEventSource.Log.WebApiMonitoringQueryBatchReceived(getAllIdentifierForLogs);

                // Retrieve all batches from the table
                using(var repo = RepositoryFactory.CreateBatchRepository(CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME)))
                {
                    var allBatches = repo.GetBatches();

                    // Log the query completion
                    GeresEventSource.Log.WebApiMonitoringQueryBatchSuccessful(getAllIdentifierForLogs, allBatches.Count());

                    // Return the batches to the client
                    var allBatchDtos = (from b in allBatches
                                        select new Batch()
                                        {
                                            Id = b.Id,
                                            BatchName = b.Name,
                                            Priority = b.Priority,
                                            RequiresDedicatedWorker = b.RequiresDedicatedWorker,
                                            Status = b.Status
                                        });

                    return allBatchDtos.AsQueryable();
                }
            }
            catch (HttpResponseException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobMonitorController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }
    }
}
