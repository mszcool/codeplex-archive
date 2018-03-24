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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Geres.Util;
using Geres.AutoScaler.Interfaces;
using Geres.Common;
using Geres.Diagnostics;

namespace AutoScaler
{
    public class WorkerRole : RoleEntryPoint
    {
        private string _currentRoleInstanceId;
        private string _currentDeploymentId;
        private TimeSpan _intervalBetweenScaleOpsInMin;
        private IAutoScalerHandler _autoScalerHandler;

        /// <summary>
        /// Role Instance Starting Method
        /// </summary>
        /// <returns></returns>
        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        /// <summary>
        /// AutoScaler Main Execution
        /// </summary>
        public override void Run()
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
                return;
            }

            try
            {
                _currentDeploymentId = RoleEnvironment.DeploymentId;
                _currentRoleInstanceId = RoleEnvironment.CurrentRoleInstance.Id;
                GeresEventSource.Log.AutoScalerWorkerStarting(_currentRoleInstanceId, _currentDeploymentId);

                // Create and Initialize the AutoScaling Handler
                _autoScalerHandler = InitializeHandler();
                RoleEnvironment.Changed += ((sender, e) =>
                {
                    InitializeHandler();
                });

                // Run the execution loop
                while (true)
                {
                    try
                    {
                        // Do the AutoScaling operation
                        _autoScalerHandler.DoAutoScaling();
                    }
                    catch (Exception ex)
                    {
                        GeresEventSource.Log.AutoScalerWorkerDoAutoScalingFailed(_currentRoleInstanceId, _currentDeploymentId, ex.Message, ex.StackTrace);
                    }

                    // Wait based on the configuration for the next scale operation
                    Thread.Sleep(_intervalBetweenScaleOpsInMin);
                }
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.AutoScalerWorkerUnhandledException(_currentRoleInstanceId, _currentDeploymentId, ex.Message, ex.StackTrace);
            }

            GeresEventSource.Log.AutoScalerWorkerStopping(_currentRoleInstanceId, _currentDeploymentId);
        }

        /// <summary>
        /// Creates and Initializes the AutoScale Handler
        /// </summary>
        /// <returns></returns>
        private IAutoScalerHandler InitializeHandler()
        {
            GeresEventSource.Log.AutoScalerWorkerHandlerInitializing(_currentRoleInstanceId, _currentDeploymentId);

            // Read configuration from the role environment
            var intervalInMinutesValue = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_AUTOSCALER_SCALE_INTERVAL));
            _intervalBetweenScaleOpsInMin = TimeSpan.FromMinutes(intervalInMinutesValue);

            // Create and initialize the AutoScale Handler
            var autoScaleInitProps = new Dictionary<string, string>();
            autoScaleInitProps.Add(Geres.AutoScaler.Constants.QUEUE_HANDLER_INIT_PROP_DEFAULT_BATCH_ID,
                                   GlobalConstants.DEFAULT_BATCH_ID);
            autoScaleInitProps.Add(Geres.AutoScaler.Constants.QUEUE_HANDLER_INIT_PROP_STORAGECONNECTION,
                                   CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME));
            autoScaleInitProps.Add(GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME,
                                   CloudConfigurationManager.GetSetting(GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME));
            autoScaleInitProps.Add(GlobalConstants.GERES_CONFIG_AUTOSCALER_CHECKFORJOBHOSTUPDATES_INTERVAL,
                                   CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_AUTOSCALER_CHECKFORJOBHOSTUPDATES_INTERVAL));

            var thisAutoScalerPolicyFactory = new CompositionAutoScalerPolicyFactory(Environment.GetEnvironmentVariable("RoleRoot") + "\\approot\\policies");
            var policy = thisAutoScalerPolicyFactory.Lookup(CloudConfigurationManager.GetSetting(Geres.AutoScaler.Constants.AUTOSCALER_POLICYTYPE));

            var scaler = Geres.AutoScaler.AutoScaleFactory.CreateAutoScaler(autoScaleInitProps, policy);

            GeresEventSource.Log.AutoScalerWorkerHandlerInitializedSuccessfully(_currentRoleInstanceId, _currentDeploymentId);

            return scaler;
        }
    }
}
