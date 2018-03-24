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
using Geres.Util;
using Microsoft.AspNet.SignalR;
using Microsoft.WindowsAzure;
using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Geres.Azure.PaaS.JobHub.Startup
{
    public static class ConfigSignalR
    {
        public static void Configure(IAppBuilder app)
        {
            string connectionString = CloudConfigurationManager.GetSetting(
                GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME);
            GlobalHost.DependencyResolver.UseServiceBus(connectionString, 
                GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_TOPICPREFIX);
            app.MapSignalR();
        }
    }
}