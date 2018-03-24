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
using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.WindowsAzure;
using Geres.Util;
using Geres.Azure.PaaS.JobHub.Startup;
using System.Web.Http;
using Geres.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.Diagnostics;

[assembly: OwinStartup(typeof(Geres.Azure.PaaS.JobProcessor.AppStartup))]

namespace Geres.Azure.PaaS.JobProcessor
{
    public class AppStartup
    {
        public void Configuration(IAppBuilder app)
        {
            try
            {
                var diagnosticsConnectionString =
                    CloudConfigurationManager.GetSetting(GlobalConstants.DIAGNOSTICS_STORAGE_CONNECTIONSTRING_CONFIGNAME);

                var level =
                    CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_DIAGNOSTICS_LEVEL);

                Geres.Diagnostics.GeresEventSource.StartDiagnostics(
                    RoleEnvironment.CurrentRoleInstance.Id,
                    diagnosticsConnectionString,
                    level
                );
            }
            catch (Exception ex)
            {
                Trace.TraceError("FATAL ERROR - unable to initialize GERES Diagnostics Component at Run()-method: {0}. Recycling role...", ex.Message);
                RoleEnvironment.RequestRecycle();
            }

            try
            {
                GeresEventSource.Log.JobHubInitializing(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);

                ConfigAuth.Configure(app);
                ConfigSignalR.Configure(app);
                GlobalConfiguration.Configure(ConfigWebApi.Configure);

                GeresEventSource.Log.JobHubInitialized(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.JobHubFailure(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, ex.Message, ex.StackTrace);
                throw ex;
            }
        }
    }
}
