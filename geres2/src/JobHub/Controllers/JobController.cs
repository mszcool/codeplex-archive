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
using Geres.Common.Interfaces.Engine;
using Geres.Diagnostics;
using Geres.Engine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Http;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Geres.Azure.PaaS.JobHub.Controllers
{
    [Authorize]
    [RoutePrefix("api/jobcontrol")]
    public class JobController : ApiController
    {
        private readonly IJobController _jobController;

        public JobController()
        {
            GeresEventSource.Log.WebApiControllerInitializing(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, "JobController");

            _jobController = EngineFactory.CreateController();

            GeresEventSource.Log.WebApiControllerInitialized(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, "JobController");
        }

        #region Job-Related operations

        [HttpPost]
        [Route("jobs/{batchId?}")]
        public string SubmitJob([FromBody] Job newJob, string batchId = "")
        {
            try
            {
                // Parameter Validations
                AssertJobParameter(newJob);

                GeresEventSource.Log.WebApiSubmitJobReceived(newJob.JobType, newJob.JobName, newJob.Parameters);
                var jobId = _jobController.SubmitJob(newJob, batchId);
                GeresEventSource.Log.WebApiSubmitJobSuccessful(jobId, newJob.JobType, newJob.JobName, newJob.Parameters);

                return jobId;
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Invalid job passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiSubmitJobInvalidParameterSubmitted(newJob.JobType, newJob.JobName, newJob.Parameters, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Job could not be submitted to system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiSubmitJobFailed(newJob.JobType, newJob.JobName, newJob.Parameters, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost]
        [Route("jobs/list/{batchId?}")]
        public List<string> SubmitJobs([FromBody] List<Job> newJobs, string batchId = "")
        {
            try
            {
                // Parameter validations
                GeresAssertionHelper.AssertNull(newJobs, "newJobs");
                foreach (var j in newJobs) AssertJobParameter(j);

                var logJobs = string.Join(", ", newJobs.Select(j => j.JobName).ToArray());
                GeresEventSource.Log.WebApiSubmitJobsReceived(newJobs.Count, logJobs, logJobs, batchId);
                var jobIds = _jobController.SubmitJobs(newJobs, batchId);
                GeresEventSource.Log.WebApiSubmitJobsSuccessful(newJobs.Count, string.Join(", ", jobIds.ToArray()), batchId);

                return jobIds;
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Invalid job passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiSubmitJobsInvalidParameterSubmitted(batchId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Job could not be submitted to system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                var logJobs = string.Join(", ", newJobs.Select(j => j.JobName).ToArray());
                GeresEventSource.Log.WebApiSubmitJobsFailed(newJobs.Count, logJobs, logJobs, batchId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete]
        [Route("jobs/{jobId}")]
        public void CancelJob(string jobId)
        {
            try
            {
                // Parameter Validations
                GeresAssertionHelper.AssertNullOrEmpty(jobId, "jobId");

                // Cancels a job
                GeresEventSource.Log.WebApiCancelJobReceived(jobId);
                _jobController.CancelJob(jobId);
                GeresEventSource.Log.WebApiCancelJobSuccessful(jobId);
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Invalid jobId passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiCancelJobInvalidParameterSubmitted(jobId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Job could not be cancelled in system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiCancelJobInvalidCancellation(jobId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Batch-related operations

        [HttpPut]
        [Route("batch")]
        public string CreateBatch([FromBody]Batch batch)
        {
            try
            {
                // Parameter validation
                AssertBatchParameter(batch);

                // Returns the generated batch-id (internal id of the system)
                GeresEventSource.Log.WebApiCreateBatchReceived(batch.BatchName, batch.Priority, batch.RequiresDedicatedWorker);
                var newBatch = _jobController.CreateBatch(batch);
                GeresEventSource.Log.WebApiCreateBatchSuccessful(batch.Id, batch.BatchName, batch.Priority, batch.RequiresDedicatedWorker);

                return newBatch.Id;
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Invalid batch passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiInvalidCreateBatchParameterReceived(batch.BatchName, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Batch could not be submitted to system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiInvalidCreateBatchOperation(batch.BatchName, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete]
        [Route("batch/close/{batchId}")]
        public void CloseBatch(string batchId)
        {
            try
            {
                // Parameter Validations
                GeresAssertionHelper.AssertNullOrEmpty(batchId, "batchId");

                // Perform the operation
                GeresEventSource.Log.WebApiBatchCloseReceived(batchId);
                _jobController.CloseBatch(batchId);
                GeresEventSource.Log.WebApiBatchCloseSuccessful(batchId);
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Invalid batch-parameter passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiInvalidCloseBatchParameterReceived(batchId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Batch could not be closed in system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiInvalidCloseBatchOperation(batchId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [HttpDelete]
        [Route("batch/delete/{batchId}")]
        public void CancelBatch(string batchId)
        {
            try
            {
                // Parameter validation
                GeresAssertionHelper.AssertNullOrEmpty(batchId, "batchId");

                // Core execution logic
                GeresEventSource.Log.WebApiBatchCancelReceived(batchId);
                _jobController.CancelBatch(batchId);
                GeresEventSource.Log.WebApiBatchCancelSuccessful(batchId);
            }
            catch (ArgumentException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Invalid batch-parameter passed into system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiInvalidCancelBatchParameterReceived(batchId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning("WebAPI - JobController -- Batch could not be cancelled in system: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiInvalidCancelBatchOperation(batchId, ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                Trace.TraceError("WebAPI - JobController -- Unknown exception occured: {0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

                GeresEventSource.Log.WebApiUnknownExceptionOccured(ex.Message, ex.StackTrace);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #region Validation Methods

        private static void AssertJobParameter(Job newJob)
        {
            GeresAssertionHelper.AssertNull(newJob, "newJob");
            GeresAssertionHelper.AssertNullOrEmpty(newJob.JobName, "newJob.JobName");
            GeresAssertionHelper.AssertNull(newJob.Parameters, "newJob.Parameters");
            GeresAssertionHelper.AssertNullOrEmptyOrWhitespace(newJob.JobType, "newJob.JobType");
            GeresAssertionHelper.AssertNullOrEmptyOrWhitespace(newJob.JobProcessorPackageName, "newJob.JobProcessorPackageName");
            GeresAssertionHelper.AssertNullOrEmptyOrWhitespace(newJob.TenantName, "newJob.TenantName");
            GeresAssertionHelper.AssertLength(newJob.TenantName, "newJob.TenantName", 1, 15);
        }

        private static void AssertBatchParameter(Batch batch)
        {
            GeresAssertionHelper.AssertNull(batch, "batch");
            GeresAssertionHelper.AssertNullOrEmpty(batch.BatchName, "batch.BatchName");
        }

        #endregion
    }
}
