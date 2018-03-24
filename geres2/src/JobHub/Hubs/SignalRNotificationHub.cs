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
using System;
using System.Threading.Tasks;
using Geres.Common.Entities;
using Microsoft.AspNet.SignalR;
using Geres.Diagnostics;
using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Geres.Azure.PaaS.JobHub.Hubs
{
    [Authorize]
    public class SignalRNotificationHub : Hub
    {
        private readonly ProgressNotificationHandler _notificationHandler;

        public SignalRNotificationHub()
        {
            GeresEventSource.Log.SignalRHubCreationInitializing(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
            _notificationHandler = new ProgressNotificationHandler();
            GeresEventSource.Log.SignalRHubCreationInitialized(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
        }

        /// <summary>
        /// Allow client to request new jobs
        /// Add client to a single user group (as recommended)
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public async Task SubscribeToJob(string jobId)
        {
            try
            {
                // Parameter Validations
                GeresAssertionHelper.AssertNullOrEmpty(jobId, "jobId");

                // register the user so that they get notifications of progress
                GeresEventSource.Log.SignalRHubSubscribeToJobRequestReceived(jobId);
                await Groups.Add(Context.ConnectionId, jobId);
                GeresEventSource.Log.SignalRHubSubscribeToJobRequestSuccessful(jobId);
            }
            catch (ArgumentException ex)
            {
                GeresEventSource.Log.SignalRHubInvalidParameterReceived(jobId, ex.Message, ex.StackTrace);
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.SignalRHubSubscribeToJobFailed(jobId, ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Allow job processor to notify of progress of the job (state is maintained in the job record)
        /// </summary>
        /// <param name="job"></param>
        public void PublishJobProgress(Job job, string progress)
        {
            try
            {
                _notificationHandler.PublishJobProgress(job, progress);
            }
            catch (Exception ex)
            {
                string eventName = "publishJobProgress";
                GeresEventSource.Log.SignalRHubEventPublishingFailed(eventName, job.JobId, job.JobType, ex.Message, ex.StackTrace);
            }
        }

        public void PublishJobComplete(Job job)
        {
            try
            {
                _notificationHandler.PublishJobComplete(job);
            }
            catch (Exception ex)
            {
                string eventName = "publishJobComplete";
                GeresEventSource.Log.SignalRHubEventPublishingFailed(eventName, job.JobId, job.JobType, ex.Message, ex.StackTrace);
            }
        }

        public void PublishJobStart(Job job)
        {
            try
            {
                _notificationHandler.PublishJobStart(job);
            }
            catch (Exception ex)
            {
                string eventName = "publishJobStart";
                GeresEventSource.Log.SignalRHubEventPublishingFailed(eventName, job.JobId, job.JobType, ex.Message, ex.StackTrace);
            }
        }
    }
}