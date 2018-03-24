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
using System.Linq;
using System.Net;
using System.Threading;
using Geres.Common;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Geres.Util;
using Geres.Engine;
using Geres.Common.Interfaces.Engine;
using Geres.Common.Entities;
using Microsoft.ServiceBus.Messaging;
using Geres.Diagnostics;
using System.Threading.Tasks;

namespace Geres.Azure.PaaS.JobProcessor
{
    public class WorkerRole : RoleEntryPoint
    {
        private int _maxNumberOfRetriesBeforeIdle = 0;
        private int _waitTimeInSecondsBetweenJobQueriesShort = 10;
        private int _waitTimeInSecondsBetweenJobQueriesLong = 20;
        private int _idlePingIntervalToAutoScalerInSeconds = 60;
        private int _autoScalerCommandCheckIntervalInSeconds = 60;
        private bool _autoScalerEnabled = false;
        private string _internalServiceBusConnectionString = string.Empty;
        private string _subscriptionName = string.Empty;
        private string _currentRoleInstanceId = string.Empty;
        private string _currentDeploymentId = string.Empty;

        /// <summary>
        /// Role Starting Event
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
        /// Main Execution Method for the worker
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
                RoleEnvironment.RequestRecycle();
            }

            try
            {
                GeresEventSource.Log.JobProcessorWorkerStarted(RoleEnvironment.CurrentRoleInstance.Id, RoleEnvironment.DeploymentId);

                // Initialize basic properties with information about the role
                _currentDeploymentId = RoleEnvironment.DeploymentId;
                _currentRoleInstanceId = RoleEnvironment.CurrentRoleInstance.Id;

                // Read general configuration settings
                _waitTimeInSecondsBetweenJobQueriesShort = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_JOBPROCESSOR_PAUSE_BETWEEN_WORKCHECK_SHORT));
                _waitTimeInSecondsBetweenJobQueriesLong = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_JOBPROCESSOR_PAUSE_BETWEEN_WORKCHECK_LONG));
                _idlePingIntervalToAutoScalerInSeconds = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_JOBPROCESSOR_IDLE_PING_INTERVAL));
                _autoScalerCommandCheckIntervalInSeconds = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_JOBPROCESSOR_AUTOSCALER_COMMANDCHECKINTERVAL));
                _autoScalerEnabled = bool.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.GERES_CONFIG_AUTOSCALER_ENABLED));
                _maxNumberOfRetriesBeforeIdle = int.Parse(CloudConfigurationManager.GetSetting(GlobalConstants.OTHER_WORKER_RETRYCOUNT_CONFIGNAME));
                _internalServiceBusConnectionString = CloudConfigurationManager.GetSetting(GlobalConstants.SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME);

                // Create the tenant manager based on the local resource path
                var localResourceForJobs = RoleEnvironment.GetLocalResource(GlobalConstants.LOCAL_RESOURCE_FOR_JOBS);
                var localResourceForJobPackageDownloads = RoleEnvironment.GetLocalResource(GlobalConstants.LOCAL_RESOURCE_FOR_TEMPJOBFILES);
                var tenantManager = EngineFactory.CreateTenantManager(localResourceForJobs.RootPath, localResourceForJobPackageDownloads.RootPath);

                // initialize the JobHandler which is actually processing the jobs with
                // the composition factory that is loading the jobs through MEF
                GeresEventSource.Log.JobProcessorWorkerInitializeJobHandler(_currentRoleInstanceId, _currentDeploymentId);
                var jobHandler = EngineFactory.CreateJobHandler(tenantManager);
                GeresEventSource.Log.JobProcessorWorkerInitializationJobHandlerCompleted(_currentRoleInstanceId, _currentDeploymentId);

                // Initialize the service bus to communicate with the AutoScaler
                var jobHostAutoScalerIntegrator = new JobHostAutoScalerIntegrator(_autoScalerEnabled, _internalServiceBusConnectionString);
                jobHostAutoScalerIntegrator.Initialize
                    (
                        _currentRoleInstanceId,
                        _currentDeploymentId,
                        _autoScalerCommandCheckIntervalInSeconds,
                        _maxNumberOfRetriesBeforeIdle,
                        _idlePingIntervalToAutoScalerInSeconds
                    );

                // When the instance is stopping, remove the subscription
                RoleEnvironment.Stopping += ((sender, e) =>
                {
                    try
                    {
                        jobHostAutoScalerIntegrator.StopAutoScaleInteraction();
                        tenantManager.DeleteTenants();
                    }
                    catch (Exception ex)
                    {
                        GeresEventSource.Log.JobProcessorWorkerRemovingAutoScalerCommandSubscriptionFailed(_currentDeploymentId, _currentDeploymentId, _subscriptionName, ex.Message, ex.StackTrace);
                    }
                });

                //
                // Start running the worker while loop and processing jobs
                //
                var retries = _maxNumberOfRetriesBeforeIdle;
                var lastTimeIdleSent = DateTime.UtcNow;
                var currentWaitTime = _waitTimeInSecondsBetweenJobQueriesShort;
                while (true)
                {
                    // wait x seconds before checking the queue again
                    Thread.Sleep(currentWaitTime);

                    // If the AutoScaler says the worker should be running, the run, otherwise stay IDLE and query the queue less often
                    if(jobHostAutoScalerIntegrator.VerifyIfWorkerShouldBeIdle())
                    {
                        // Increase the time between checking the status
                        currentWaitTime = _waitTimeInSecondsBetweenJobQueriesLong;
                    }
                    else
                    {
                        // The worker is not idle, process the queue
                        // Check whether there is a job to process, if a job is processed then true is returned
                        GeresEventSource.Log.JobProcessorWorkerStartProcessJobStart(_currentRoleInstanceId, _currentDeploymentId, jobHostAutoScalerIntegrator.AssignedDedicatedBatchId);
                        var result = jobHandler.Process(jobHostAutoScalerIntegrator.AssignedDedicatedBatchId);
                        GeresEventSource.Log.JobProcessorWorkerProcessJobResultReceived(_currentRoleInstanceId, _currentDeploymentId, jobHostAutoScalerIntegrator.AssignedDedicatedBatchId, result);

                        // If there was NO job on the queue, register it as an empty retry
                        if (!result)
                        {
                            // Register an empty processing cycle
                            jobHostAutoScalerIntegrator.RegisterRetryProcessing();

                            // Leverage the empty processing cycle for clean-up tasks
                            tenantManager.DeleteTenants();

                            // No job processed, set the current wait-time to long  
                            currentWaitTime = _waitTimeInSecondsBetweenJobQueriesLong;
                        }
                        else
                        {
                            // Job processed, set the wait-time to short
                            currentWaitTime = _waitTimeInSecondsBetweenJobQueriesShort;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.JobProcessorWorkerUnhandledExceptionOccured(_currentRoleInstanceId, _currentDeploymentId, ex.Message, ex.StackTrace);
            }

            GeresEventSource.Log.JobProcessorWorkerStopping(_currentRoleInstanceId, _currentDeploymentId);
        }
    }
}
