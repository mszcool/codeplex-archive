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
using Geres.ClientSdk.Core.Interfaces;
using Geres.ClientSdk.Core.Proxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.ClientSdk.Core
{
    public abstract class GeresServiceClient : IGeresServiceClient
    {
        protected string AuthenticationToken { get; set; }

        public GeresServiceClient(string baseUrl)
        {
            _BaseUrl = BaseUrl;

            _managementServiceClient = new WebApiManagementClient(baseUrl);
            _monitoringServiceClient = new WebApiMonitoringClient(baseUrl);
            _notificationClient = new SignalRNotificationServiceClient(baseUrl);
        }

        #region IGeresServiceClient Implementation

        private string _BaseUrl = default(string);
        public string BaseUrl
        {
            get { return _BaseUrl; }
        }

        private bool _Authenticated = false;
        public bool Authenticated
        {
            get { return _Authenticated; }
        }

        private WebApiManagementClient _managementServiceClient;
        public IManagementServiceClient Management
        {
            get { return _managementServiceClient; }
        }

        private WebApiMonitoringClient _monitoringServiceClient;
        public IMonitoringServiceClient Monitoring
        {
            get { return _monitoringServiceClient; }
        }

        private SignalRNotificationServiceClient _notificationClient;
        public INotificationClient Notifications
        {
            get { return _notificationClient; }
        }

        public async Task<bool> Authenticate(string clientId, string clientSecret)
        {
            _Authenticated = false;
            
            AuthenticationToken = await GetAuthenticationToken(clientId, clientSecret);
            if (!string.IsNullOrEmpty(AuthenticationToken))
                _Authenticated = true;

            _managementServiceClient.SetAuthenticationToken(AuthenticationToken);
            _monitoringServiceClient.SetAuthenticationToken(AuthenticationToken);
            _notificationClient.SetAuthenticationToken(AuthenticationToken);

            return _Authenticated;
        }

        #endregion

        #region Virtual methods that must be overridden by concrete implementations

        protected abstract Task<string> GetAuthenticationToken(string clientId, string clientSecret);

        #endregion
    }
}
