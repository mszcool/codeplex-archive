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
using Geres.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace Geres.Azure.PaaS.JobHub
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            try
            {
                GeresEventSource.Log.JobHubSignalRServiceBusBridgeInitializing(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
                var bridge = new JobNotificationServiceBusSignalRBridge();
                bridge.RunSignalRBridgeLoop();
                GeresEventSource.Log.JobHubSignalRServiceBusBridgeInitialized(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);
            }
            catch (Exception ex)
            {
                // Don't make the whole website fail just because of SignalR not working
                // Log the event so that operations can look into it, but keep the system up'n'running
                GeresEventSource.Log.JobHubSignalRServiceBusBridgeInitializationFailed(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId, ex.Message, ex.StackTrace);
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}