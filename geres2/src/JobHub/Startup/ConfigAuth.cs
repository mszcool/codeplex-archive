using Microsoft.WindowsAzure;
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
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Geres.Azure.PaaS.JobHub.Startup
{
    public static class ConfigAuth
    {
        public static void Configure(IAppBuilder app)
        {
            app.UseWindowsAzureActiveDirectoryBearerAuthentication
                (
                    new Microsoft.Owin.Security.ActiveDirectory.WindowsAzureActiveDirectoryBearerAuthenticationOptions()
                    {
                        Tenant = CloudConfigurationManager.GetSetting(Geres.Util.GlobalConstants.AZUREAD_ADTENANT_CONFIG),
                        Audience = CloudConfigurationManager.GetSetting(Geres.Util.GlobalConstants.AZUREAD_ADAUDIENCEURI_CONFIG)
                    }
                );
        }
    }
}