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
using Geres.Common.Entities;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Geres.Azure.PaaS.JobHub.Hubs;

namespace Geres.Azure.PaaS.JobHub
{
    public class ProgressNotificationHandler
    {
        /// <summary>
        /// Allow job processor to notify of progress of the job (state is maintained in the job record)
        /// </summary>
        /// <param name="job"></param>
        public void PublishJobProgress(Job job, string progress)
        {
            GlobalHost.ConnectionManager
                .GetHubContext<SignalRNotificationHub>()
                .Clients.Group(job.JobId).publishJobProgress(job, progress);
        }

        public void PublishJobComplete(Job job)
        {
            GlobalHost.ConnectionManager
                .GetHubContext<SignalRNotificationHub>()
                .Clients.Group(job.JobId).publishJobComplete(job);
        }

        public void PublishJobStart(Job job)
        {
            GlobalHost.ConnectionManager
                .GetHubContext<SignalRNotificationHub>()
                .Clients.Group(job.JobId).publishJobStart(job);
        }
    }
}