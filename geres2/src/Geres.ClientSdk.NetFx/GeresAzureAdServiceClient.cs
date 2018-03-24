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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.ClientSdk.NetFx
{
    public class GeresAzureAdServiceClient : Geres.ClientSdk.Core.GeresServiceClient
    {
        private string _azureAdTenant;
        private string _azureAdGeresWebApiId;
        private AuthenticationContext _adalAuthContext;
        private AuthenticationResult _adalAuthResult;
        
        public GeresAzureAdServiceClient(string baseUrl, string azureAdTenant, string azureAdGeresWebApiId)
            : base(baseUrl)
        {
            _azureAdTenant = azureAdTenant;
            _azureAdGeresWebApiId = azureAdGeresWebApiId;
            _adalAuthContext = new AuthenticationContext(_azureAdTenant);
        }

        protected override async Task<string> GetAuthenticationToken(string clientId, string clientSecret)
        {
            bool authSucceeded = false;

            var authTask = Task.Run(() =>
            {
                var clientCreds = new ClientCredential(clientId, clientSecret);
                _adalAuthResult = _adalAuthContext.AcquireToken
                                        (
                                            _azureAdGeresWebApiId,
                                            clientCreds
                                        );
                authSucceeded = true;
            });
            await Task.WhenAll(authTask);

            if (authSucceeded)
                return _adalAuthResult.AccessToken;
            else if (authTask.Exception != null)
                throw authTask.Exception;
            else
                return null;
        }
    }
}
