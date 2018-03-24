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
using Geres.ClientSdk.Core.Events;
using Geres.ClientSdk.Core.Interfaces;
using Geres.Common.Entities;
using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.ClientSdk.Core.Proxies
{
    internal class SignalRNotificationServiceClient : INotificationClient
    {
        private readonly string _signalRHubName = "SignalRNotificationHub";

        private HubConnection _signalRConnection = null;
        private IHubProxy _signalRProxy = null;

        internal SignalRNotificationServiceClient(string baseUrl)
        {
            _signalRConnection = new HubConnection(baseUrl);
            _signalRConnection.Error += (ex) =>
            {
                OnExceptionOccured(ex);
            };
            _signalRConnection.StateChanged += (statusInfo) =>
            {
                OnStatusChanged(statusInfo.OldState.ToString(), statusInfo.NewState.ToString());
            };
        }

        internal void SetAuthenticationToken(string authenticationToken)
        {
            var authHeaderKey = "Authorization";

            if (_signalRConnection.Headers.ContainsKey(authHeaderKey))
                _signalRConnection.Headers.Remove(authHeaderKey);

            _signalRConnection.Headers.Add
                (
                    authHeaderKey, 
                    string.Format
                        (
                            "Bearer {0}", 
                            authenticationToken
                        )
                 );
        }

        #region INotificationClient interface implementation

        public Task Connect()
        {
            _signalRProxy = _signalRConnection.CreateHubProxy(_signalRHubName);

            _signalRProxy.On<Job>("publishJobStart", (Job) =>
            {
                OnJobStarted(Job);
            });

            _signalRProxy.On<Job, string>("publishJobProgress", (Job, progress) =>
            {
                OnJobProgressed(Job, progress);
            });

            _signalRProxy.On<Job>("publishJobComplete", (job) =>
            {
                OnJobCompleted(job);
            });

            return _signalRConnection.Start();
        }

        public void Disconnect()
        {
            _signalRConnection.Stop();
        }

        public void SubscribeToJob(string jobId)
        {
            _signalRProxy.Invoke("SubscribeToJob", jobId);
        }

        public event EventHandler<StatusChangeEventArgs> StatusChanged;

        public event EventHandler<Exception> ExceptionOccured;

        public event EventHandler<Events.JobEventArgs> JobStarted;

        public event EventHandler<Events.JobEventArgs> JobProgressed;

        public event EventHandler<Events.JobEventArgs> JobCompleted;

        #endregion

        #region Event Fire methods

        public virtual void OnStatusChanged(string oldStatus, string newStatus)
        {
            if (StatusChanged != null)
                StatusChanged(this, new StatusChangeEventArgs(oldStatus, newStatus));
        }

        public virtual void OnExceptionOccured(Exception ex)
        {
            if (ExceptionOccured != null)
                ExceptionOccured(this, ex);
        }

        public virtual void OnJobStarted(Job job)
        {
            if (JobStarted != null)
                JobStarted(this, new JobEventArgs(job, null));
        }

        public virtual void OnJobProgressed(Job job, string progress)
        {
            if (JobProgressed != null)
                JobProgressed(this, new JobEventArgs(job, progress));
        }

        public virtual void OnJobCompleted(Job job)
        {
            if (JobCompleted != null)
                JobCompleted(this, new JobEventArgs(job, null));
        }

        #endregion
    }
}
