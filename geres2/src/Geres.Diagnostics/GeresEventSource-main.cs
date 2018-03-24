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
using System.Linq;
using System.Web;
using System.Diagnostics.Tracing;
using Microsoft.Practices.EnterpriseLibrary.SemanticLogging;

/* How to use the Semantic Logging Application Block (SLAB)
 * 
 *      - use Nuget to add EnterpriseLibrary.SemanticLogging
 *      - add using Microsoft.Practices.EnterpriseLibrary.SemanticLogging/SemanticLogging.Formatters/SemanticLogging.Sinks;
 *      - Add project reference to SLAB and using slab; into your project
 *      - Initialize SLAB in yoru prioject eg global.asax, etc
 *      - Decorate your code with calls to it via TedEventSource.Log.xxxx
 *      - Add new source messages and keywords to this source code file as needed
 *      
 *      for more info - http://msdn.microsoft.com/en-us/library/dn440729(v=pandp.60).aspx 
 */

namespace Geres.Diagnostics
{
    [EventSource(Name = "Microsoft-TED-GenericResourceScheduler2")]
    public class GeresEventSource : EventSource
    {
        public const string GERES_DIAGNOSTIC_LEVEL_DETAILED = "Detailed";
        public const string GERES_DIAGNOSTIC_LEVEL_ERRORSONLY = "ImportantOnly";

        public class Keywords
        {
            // 64-bit integer, so 64 keywords possible
            public const EventKeywords Application = (EventKeywords)1L;
            public const EventKeywords Diagnostics = (EventKeywords)2L;
            public const EventKeywords UserInterface = (EventKeywords)4L;
            public const EventKeywords General = (EventKeywords)8L;
            public const EventKeywords Infrastructure = (EventKeywords)16L;
        }

        public class Tasks
        {
            // should we use this to denote the different functional areas of the system?

            public const EventTask JobQueue = (EventTask)1;
            public const EventTask JobProcessing = (EventTask)2;
            public const EventTask JobHub = (EventTask)3;
            public const EventTask WebAPIJobBatchOps = (EventTask)4;
            public const EventTask WebAPIJobBatchMonitoring = (EventTask)5;
            public const EventTask AutoScaler = (EventTask)6;
            public const EventTask Notifications = (EventTask)7;
            public const EventTask JobController = (EventTask)8;
            public const EventTask JobHost = (EventTask)9;
        }

        // create static instance of log
        private static GeresEventSource _log = null;
        public static GeresEventSource Log
        {
            get
            {
                if (_log == null)
                    _log = new GeresEventSource();
                return _log;
            }
        }

        private static ObservableEventListener _slabEventListener = null;
        public static void StartDiagnostics(string roleInstanceName, string storageConnectionString, string level)
        {
            var listener = new ObservableEventListener();

            if (level == GERES_DIAGNOSTIC_LEVEL_ERRORSONLY)
            {
                listener.EnableEvents
                (
                    GeresEventSource.Log,
                    EventLevel.Error | EventLevel.Critical,
                    GeresEventSource.Keywords.Application | GeresEventSource.Keywords.Diagnostics |
                    GeresEventSource.Keywords.General | GeresEventSource.Keywords.Infrastructure |
                    GeresEventSource.Keywords.UserInterface
                );
            }
            else
            {
                listener.EnableEvents
                (
                    GeresEventSource.Log,
                    EventLevel.LogAlways,
                    GeresEventSource.Keywords.Application | GeresEventSource.Keywords.Diagnostics |
                    GeresEventSource.Keywords.General | GeresEventSource.Keywords.Infrastructure |
                    GeresEventSource.Keywords.UserInterface
                );
            }

            listener.LogToWindowsAzureTable
                (
                    roleInstanceName,
                    storageConnectionString
                );
            _slabEventListener = listener;
        }

        /* You can use the Level parameter of the Event attribute to specify the severity level of the message. 
         * The EventLevel enumeration determines the available log levels: 
         *  Verbose (5), Informational (4), Warning (3), Error (2), Critical (1), and LogAlways (0). 
         * Informational is the default logging level when not specified. When you enable an event source in your application, 
         * you can specify a log level, and the event source will log all log messages with same or lower log level. 
         * For example, if you enable an event source with the warning log level, all log methods with a level parameter value 
         * of Warning, Error, Critical, and LogAlways will be able to write log messages
         */


        // events

        #region JobHub General Events

        [Event(101, Message = "Initializing Job Hub: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.JobHub, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubInitializing(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                this.WriteEvent(101, roleInstanceId, deploymentId);
        }

        [Event(102, Message = "Job Hub Initialized Successfully: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.JobHub, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubInitialized(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                this.WriteEvent(102, roleInstanceId, deploymentId);
        }

        [Event(103, Message = "Job Hub Failed to Initialize: Role Instance: {0}, Deployment Id: {1}, Reason {2} - {3}",
            Task = Tasks.JobHub, Keywords = Keywords.Infrastructure, Level = EventLevel.Critical)]
        public void JobHubFailure(string roleInstanceId, string deploymentId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(103, roleInstanceId, deploymentId, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region WebAPI General Events

        [Event(201, Message = "WebApi Controller Initializing: Role Instance: {0}, Deployment Id: {1}, Name: {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void WebApiControllerInitializing(string roleInstanceId, string deploymentId, string controllerName)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(201, roleInstanceId, deploymentId, controllerName);
        }

        [Event(202, Message = "WebApi Initialized Successfully: Role Instance: {0}, Deployment Id: {1}, Name: {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void WebApiControllerInitialized(string roleInstanceId, string deploymentId, string controllerName)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(202, roleInstanceId, deploymentId, controllerName);
        }

        [Event(203, Message = "WebApi Unknown Exception {0} occured: {1}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Infrastructure, Level = EventLevel.Critical)]
        public void WebApiUnknownExceptionOccured(string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(203, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region Web API Job Management Events

        [Event(301, Message = "Submit Job Received Type:{0}; Name:{1}; Parameters:{2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiSubmitJobReceived(string jobType, string jobName, string jobParameters)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(301, jobType, jobName, jobParameters);
        }

        [Event(302, Message = "Submit Job Successful: Type:{0}; Id:{3} Name:{1}; Parameters:{2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiSubmitJobSuccessful(string jobId, string jobType, string jobName, string jobParameters)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(302, jobType, jobName, jobParameters, jobId);
        }

        [Event(303, Message = "Submit Job Failed: Type: {0}; Name: {1}; Parameters: {2}; Reason: {3} - {4}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiSubmitJobFailed(string jobType, string jobName, string jobParameters, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(303, jobType, jobName, jobParameters, exceptionMessage, exceptionStackTrace);
        }

        [Event(304, Message = "Submit Batch Received: BatchId: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiSubmitJobsReceived(int jobsCount, string jobNames, string jobTypes, string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(304, batchId);
        }

        [Event(305, Message = "Submit Batch Successful: BatchId: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiSubmitJobsSuccessful(int jobsCount, string jobIds, string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(305, batchId);
        }

        [Event(306, Message = "Submit Batch Failed: BatchId: {0}; Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiSubmitJobsFailed(int jobsCount, string jobNames, string jobTypes, string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(307, batchId, exceptionMessage, exceptionStackTrace);
        }

        [Event(308, Message = "Submit Job Failed: Type: {0}, Name: {1}, Parameters: {2}, Reason:{3} - {4}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiSubmitJobInvalidParameterSubmitted(string jobType, string jobName, string jobParameters, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(308, jobType, jobName, jobParameters, exceptionMessage, exceptionStackTrace);
        }

        [Event(309, Message = "Cancel Job Received: Job Id: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiCancelJobReceived(string jobId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(309, jobId);
        }

        [Event(310, Message = "Cancel Job Received Successful: JobId: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiCancelJobSuccessful(string jobId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(310, jobId);
        }

        [Event(311, Message = "Cancel Job Receievd Failed: JobId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiCancelJobInvalidCancellation(string jobId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(311, jobId, exceptionMessage, exceptionStackTrace);
        }

        [Event(312, Message = "Cancel Job Received Failed: JobId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiCancelJobInvalidParameterSubmitted(string jobId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(312, jobId, exceptionMessage, exceptionStackTrace);
        }

        [Event(313, Message = "Submit Batch Failed: BatchId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiSubmitJobsInvalidParameterSubmitted(string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(313, batchId, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region Web API Batch Management Events

        [Event(401, Message = "Create Batch Received: Name: {0}, Priority: {1}, Requires Dedicated Batch: {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiCreateBatchReceived(string batchName, int batchPriority, bool batchRequiresDedicatedWorker)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(401, batchName, batchPriority.ToString(), batchRequiresDedicatedWorker.ToString());
        }

        [Event(402, Message = "Create Batch Successful: BatchId: {0}, Name: {1}, Priority: {2}, Requires Dedicated Batch: {3}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiCreateBatchSuccessful(string batchId, string batchName, int batchPriority, bool batchRequiresDedicatedWorker)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(402, batchId, batchName, batchPriority.ToString(), batchRequiresDedicatedWorker.ToString());
        }

        [Event(403, Message = "Create Batch Failed: BatchName: {0}; Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiInvalidCreateBatchParameterReceived(string batchName, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(403, batchName, exceptionMessage, exceptionStackTrace);
        }

        [Event(404, Message = "Create Batch Failed: BatchName: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiInvalidCreateBatchOperation(string batchName, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(404, batchName, exceptionMessage, exceptionStackTrace);
        }

        [Event(405, Message = "Close Batch Received BatchId: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiBatchCloseReceived(string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(405, batchId);
        }

        [Event(406, Message = "Close Batch Successful BatchId: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiBatchCloseSuccessful(string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(406, batchId);
        }

        [Event(407, Message = "Close Batch Failed: BatchId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiInvalidCloseBatchParameterReceived(string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(407, batchId, exceptionMessage, exceptionStackTrace);
        }


        [Event(408, Message = "Close Batch Failed: BatchId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiInvalidCloseBatchOperation(string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(408, batchId, exceptionMessage, exceptionStackTrace);
        }


        [Event(409, Message = "Cancel Batch Received BatchId: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiBatchCancelReceived(string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(409, batchId);
        }


        [Event(410, Message = "Cancel Batch Successful BatchId: {0}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiBatchCancelSuccessful(string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(410, batchId);
        }


        [Event(411, Message = "Cancel Batch Failed: BatchId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiInvalidCancelBatchParameterReceived(string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(411, batchId, exceptionMessage, exceptionStackTrace);
        }

        [Event(412, Message = "Cancel Batch Failed: BatchId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchOps, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiInvalidCancelBatchOperation(string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(412, batchId, exceptionMessage, exceptionStackTrace);
        }


        #endregion

        #region Web API Monitoring Related Events

        [Event(501, Message = "Monitoring Job Failed: JobId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiJobMonitoringInvalidParameterReceived(string jobId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(501, jobId, exceptionMessage, exceptionStackTrace);
        }

        [Event(502, Message = "Monitoring Job Failed: BatchId: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void WebApiBatchMonitoringInvalidParameterReceived(string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(502, batchId, exceptionMessage, exceptionStackTrace);
        }

        [Event(503, Message = "Job Query Received: Job Id: {0}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiMonitoringQueryJobReceived(string jobId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(503, jobId);
        }

        [Event(504, Message = "Job Query Successful: JobId: {0}, JobType: {1}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiMonitoringQueryJobSuccessful(string jobId, string jobType)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(504, jobId, jobType);
        }

        [Event(505, Message = "Batch Query Received: BatchId: {0}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiMonitoringQueryBatchReceived(string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(505, batchId);
        }

        [Event(506, Message = "Batch Query Successful: BatchId: {0}, JobCount: {1}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void WebApiMonitoringQueryBatchSuccessful(string batchId, int jobsCount)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(506, batchId, jobsCount.ToString());
        }

        #endregion

        #region SignalR Hub Related Events

        [Event(601, Message = "SignalR Hub Initializing: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void SignalRHubCreationInitializing(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(601, roleInstanceId, deploymentId);
        }

        [Event(602, Message = "SignalR Hub Initialized: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void SignalRHubCreationInitialized(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(602, roleInstanceId, deploymentId);
        }

        [Event(603, Message = "Subscribe to Job Notifications Received: Job Id: {0}",
            Task = Tasks.Notifications, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void SignalRHubSubscribeToJobRequestReceived(string jobId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(603, jobId);
        }

        [Event(604, Message = "Subscribe to Job Notifications Successful: Id: {0}",
            Task = Tasks.Notifications, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void SignalRHubSubscribeToJobRequestSuccessful(string jobId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(604, jobId);
        }

        [Event(605, Message = "Subscribe to Job Notifications Failed: Id: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void SignalRHubSubscribeToJobFailed(string jobId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(605, jobId, exceptionMessage, exceptionStackTrace);
        }

        [Event(606, Message = "Subscribe to Job Notifications Failed: Id: {0}, Reason: {1} - {2}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void SignalRHubInvalidParameterReceived(string jobId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(606, jobId, exceptionMessage, exceptionStackTrace);
        }

        [Event(607, Message = "Publishing Job Notification Failed: Id: {0}, JobType: {1}, EventName: {2}, Reason: {3} - {4}",
            Task = Tasks.WebAPIJobBatchMonitoring, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void SignalRHubEventPublishingFailed(string eventName, string jobId, string jobType, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(607, jobId, jobType, eventName, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region SignalR ServiceBus Bridge Events

        [Event(701, Message = "SignalR Service Bus Bridge Initializing: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeInitializing(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(701, roleInstanceId, deploymentId);
        }

        [Event(702, Message = "SignalR Service Bus Bridge Initialized: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeInitialized(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(702, roleInstanceId, deploymentId);
        }

        [Event(703, Message = "SignalR Service Bus Bridge Failed: Role Instance: {0}, Deployment Id: {1}, Reason: {2} : {3}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Critical)]
        public void JobHubSignalRServiceBusBridgeInitializationFailed(string roleInstanceId, string deploymentId, string exceptionMessage, string exceptionStackTrace)
        {
            if (IsEnabled())
                WriteEvent(703, roleInstanceId, deploymentId, exceptionMessage, exceptionStackTrace);
        }

        [Event(704, Message = "SignalR Service Bus Bridge Read Config: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeReadConfig(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(704, roleInstanceId, deploymentId);
        }

        [Event(705, Message = "SignalR Service Bus Bridge Read Config Successful: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeReadConfigSuccessful(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(705, roleInstanceId, deploymentId);
        }

        [Event(706, Message = "SignalR Service Bus Bridge Topic Initializing: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeInitializingTopicsAndSubscriptions(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(706, roleInstanceId, deploymentId);
        }


        [Event(707, Message = "SignalR Service Bus Bridge Creating Topic: Role Instance: {0}, Deployment Id: {1}, Name: {2}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeCreatingTopic(string roleInstanceId, string deploymentId, string topicName)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(707, roleInstanceId, deploymentId, topicName);
        }

        [Event(708, Message = "SignalR Service Bus Bridge Creating Topic: Role Instance: {0}, Deployment Id: {1}, Name:{2}, FAILED {3} - {4}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Error)]
        public void JobHubSignalRServiceBusBridgeTopicCreationFailed(string roleInstanceId, string deploymentId, string topicName, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(708, roleInstanceId, deploymentId, topicName, exceptionMessage, exceptionStackTrace);
        }

        [Event(709, Message = "SignalR Service Bus Bridge Creating Subscriptions: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeCreatingSubscriptions(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(709, roleInstanceId, deploymentId);
        }

        [Event(710, Message = "SignalR Service Bus Bridge Creating Subscriptions Failed: Role Instance: {0}, Deployment Id: {1}, Reason: {2} : {3}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Error)]
        public void JobHubSignalRServiceBusBridgeCreateSubscriptionsFailed(string roleInstanceId, string deploymentId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(710, roleInstanceId, deploymentId, exceptionMessage, exceptionStackTrace);
        }

        [Event(711, Message = "SignalR Service Bus Bridge Creating Subscriptions Successful: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeCreatingSubscriptionsSuccessful(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(711, roleInstanceId, deploymentId);
        }

        [Event(712, Message = "SignalR Service Bus Bridge Creating Topic Successful: Role Instance: {0}, Deployment Id: {1}, Name: {2}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeCreateTopicSuccessful(string roleInstanceId, string deploymentId, string topicName)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(712, roleInstanceId, deploymentId, topicName);
        }

        [Event(713, Message = "SignalR Service Bus Bridge Topics and Subscriptions Initialized Successful: Role Instance: {0}, Deployment Id: {1}",
            Task = Tasks.Notifications, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeTopicsAndSubscriptionsInitializedSuccessful(string roleInstanceId, string deploymentId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(713, roleInstanceId, deploymentId);
        }

        [Event(714, Message = "SignalR Service Bus Bridge Notification Message Received",
            Task = Tasks.Notifications, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobHugSignalRServiceBusBridgeNotificationReceived(string messageId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(714, messageId);
        }

        [Event(715, Message = "SignalR Service Bus Bridge Notification Message Parsed: JobId:{0}, JobType:{1}, JobStatus:{2}",
            Task = Tasks.Notifications, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeNotificationReceivedJobParsed(string jobId, string jobType, string jobStatus)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(715, jobId, jobType, jobStatus);
        }

        [Event(716, Message = "SignalR Service Bus Bridge Notification Processed: JobId: {0}, JobType: {1}, JobStatus: {2}",
            Task = Tasks.Notifications, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobHubSignalRServiceBusBridgeNotificationSentToSignalRHub(string jobId, string jobType, string jobStatus)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(716, jobId, jobType, jobStatus);
        }

        [Event(717, Message = "SignalR Service Bus Bridge Handle Message Failed {1} - {2}",
            Task = Tasks.Notifications, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void JobHugSignalRServiceBusBridgeNotificationHandleMessageFailed(string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(717, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region JobProcessor Worker-related Events

        [Event(801, Message = "Job Processor Role Instance: {0}, Deployment: {1} Initializing",
            Task = Tasks.JobProcessing, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobProcessorWorkerStarted(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(801, roleInstanceId, deploymentId);
        }

        [Event(802, Message = "Job Processor Role Instance: {0}, Deployment: {1} Handler Initializing",
            Task = Tasks.JobProcessing, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobProcessorWorkerInitializeJobHandler(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(802, roleInstanceId, deploymentId);
        }

        [Event(803, Message = "Job Processor Role Instance: {0}, Deployment: {1} Handler Initialized",
            Task = Tasks.JobProcessing, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobProcessorWorkerInitializationJobHandlerCompleted(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(803, roleInstanceId, deploymentId);
        }

        [Event(804, Message = "Job Processor Role Instance: {0}, Deployment: {1} Service Bus Initializing",
            Task = Tasks.JobProcessing, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobProcessorWorkerInitializingAutoScalerServiceBus(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(804, roleInstanceId, deploymentId);
        }

        [Event(805, Message = "Job Processor Role Instance: {0}, Deployment: {1} Service Bus Initialized",
            Task = Tasks.JobProcessing, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobProcessorWorkerInitializingAutoScalerServiceBusCompleted(string roleInstanceId, string deploymentId, string subscriptionName)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(805, roleInstanceId, deploymentId);
        }

        [Event(806, Message = "Job Processor Role Instance: {0}, Deployment: {1} Command Received",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobProcessorWorkerAutoScalerCommandReceived(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(806, roleInstanceId, deploymentId);
        }

        [Event(807, Message = "Job Processor Role Instance: {0}, Deployment: {1} Processing Command Failed: {2} - {3}",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void JobProcessorWorkerAutoScalerCommandReceivedProcessingError(string roleInstanceId, string deploymentId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(807, roleInstanceId, deploymentId, exceptionMessage, exceptionStackTrace);
        }

        [Event(808, Message = "Job Processor Role Instance: {0}, Deployment: {1} Sending IsReady Command",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.LogAlways)]
        public void JobProcessorWorkerSendingInstanceReadyToAutoScaler(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(808, roleInstanceId, deploymentId);
        }

        [Event(809, Message = "Job Processor Role Instance: {0}, Deployment: {1} IsReady Command Sent Successfully",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobProcessorWorkerSendingInstanceReadyToAutoScalerCompleted(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(809, roleInstanceId, deploymentId);
        }

        [Event(810, Message = "Job Processor Role Instance: {0}, Deployment: {1} Removing Service Bus Subscription {2}",
            Task = Tasks.JobProcessing, Keywords = Keywords.Infrastructure, Level = EventLevel.Informational)]
        public void JobProcessorWorkerRemovingAutoScalerCommandSubscription(string roleInstanceId, string deploymentId, string subscriptionName)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Infrastructure))
                WriteEvent(810, roleInstanceId, deploymentId, subscriptionName);
        }

        [Event(811, Message = "Job Processor Before Wait for Unblock Reset Event: Role Instance - {0}, Deployment: {1} Idle",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.LogAlways)]
        public void JobProcessorWorkerWaitingForUnblockResetEvent(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(811, roleInstanceId, deploymentId);
        }

        [Event(812, Message = "Job Processor Role Instance: {0}, Deployment: {1} Received Start Running command",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobProcessorWorkerReceivedAutoScalerStartRunning(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(812, roleInstanceId, deploymentId);
        }

        [Event(813, Message = "Job Processor Role Instance: {0}, Deployment: {1}{2} Discovering Job",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobProcessorWorkerStartProcessJobStart(string roleInstanceId, string deploymentId, string dedicatedBatchId)
        {
            var dedicatedBatchMessage = string.Empty;

            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
            {
                if (!string.IsNullOrEmpty(dedicatedBatchId))
                    dedicatedBatchMessage = string.Format(", Dedicated Batch: {0}", dedicatedBatchId);

                WriteEvent(813, roleInstanceId, deploymentId, dedicatedBatchMessage);
            }
        }

        [Event(814, Message = "Job Processor Role Instance: {0}, Deployment: {1}{2} Job Discovered",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobProcessorWorkerProcessJobResultReceived(string roleInstanceId, string deploymentId, string dedicatedBatchId, bool result)
        {
            // only send the message if a job was successfully found and processed
            if (result)
            {
                var dedicatedBatchMessage = string.Empty;

                if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                {
                    if (!string.IsNullOrEmpty(dedicatedBatchId))
                        dedicatedBatchMessage = string.Format(", Dedicated Batch: {0}", dedicatedBatchId);

                    WriteEvent(814, roleInstanceId, deploymentId, dedicatedBatchMessage);
                }
            }
        }

        [Event(815, Message = "Job Processor Role Instance: {0}, Deployment: {1} ready to send Idle Command",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobProcessorWorkerAutoScalerNotifyIdleStatus(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(815, roleInstanceId, deploymentId);
        }

        [Event(816, Message = "Job Processor Role Instance: {0}, Deployment: {1} Blocking waiting for Command or Delete",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.LogAlways)]
        public void JobProcessorWorkerBlockUntilAutoScalerRelease(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(816, roleInstanceId, deploymentId);
        }

        [Event(817, Message = "Job Processor Role Instance: {0}, Deployment: {1} Failed: {2} - {3}",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void JobProcessorWorkerUnhandledExceptionOccured(string roleInstanceId, string deploymentId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(817, roleInstanceId, deploymentId, exceptionMessage, exceptionStackTrace);
        }

        [Event(818, Message = "Job Processor Role Instance: {0}, Deployment: {1} Stopping",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void JobProcessorWorkerStopping(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(818, roleInstanceId, deploymentId);
        }

        [Event(819, Message = "Job Processor Role Instance: {0}, Deployment: {1} Removing Subscription {2} Falied: {3} - {4}",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void JobProcessorWorkerRemovingAutoScalerCommandSubscriptionFailed(string roleInstanceId, string deploymentId, string subscriptionName, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(819, roleInstanceId, deploymentId, subscriptionName, exceptionMessage, exceptionStackTrace);
        }

        [Event(820, Message = "Job Processor failed listening to ServiceBus for receiving AutoScaler Commands! Role Instance: {0}, Deployment: {1} ServiceBus {2} Exception: {3} - {4}",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void JobProcessorWorkerAutoScalerListenForCommandsFailed(string roleInstanceId, string deploymentId, string serviceBusDetails, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(820, roleInstanceId, deploymentId, serviceBusDetails, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region AutoScaler Worker-related Events

        [Event(901, Message = "AutoScaler Role Instance: {0}, Deployment: {1} Initializing",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void AutoScalerWorkerStarting(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(901, roleInstanceId, deploymentId);
        }

        [Event(902, Message = "Auto Scaler Role Instance: {0}, Deployment: {1} Initialized Successfully",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void AutoScalerWorkerHandlerInitializedSuccessfully(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(902, roleInstanceId, deploymentId);
        }

        [Event(903, Message = "Auto Scaler Role Instance: {0}, Deployment: {1} Handler Initializing",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void AutoScalerWorkerHandlerInitializing(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(903, roleInstanceId, deploymentId);
        }

        [Event(904, Message = "Auto Scaler Role Instance: {0}, Deployment: {1} Unhandled Exception {2} - {3}",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void AutoScalerWorkerUnhandledException(string roleInstanceId, string deploymentId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(904, roleInstanceId, deploymentId, exceptionMessage, exceptionStackTrace);
        }

        [Event(905, Message = "Auto Scaler Role Instance: {0}, Deployment: {1} Stopping",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Warning)]
        public void AutoScalerWorkerStopping(string roleInstanceId, string deploymentId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(905, roleInstanceId, deploymentId);
        }

        [Event(906, Message = "Auto Scaler Role Instance: {0}, Deployment: {1} AutoScaling failed {2} - {3}",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void AutoScalerWorkerDoAutoScalingFailed(string roleInstanceId, string deploymentId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(906, roleInstanceId, deploymentId, exceptionMessage, exceptionStackTrace);
        }

        [Event(907, Message = "Auto Scaler failed updating Job Host Status Table or Sending message to Job Host: Role Instance: {0}, Deployment: {1}, JobHost: {2}, Exception {3} - {4}",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void AutoScalerWorkerFailedManageJobHost(string roleInstanceId, string deploymentId, string jobHostInstanceId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(907, roleInstanceId, deploymentId, jobHostInstanceId, exceptionMessage, exceptionStackTrace);
        }

        [Event(908, Message = "Auto Scaler received a command from a job host: Role Instance: {0}, Deployment: {1}, Message ID: {2}, Sender Role InstanceId {3}",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.LogAlways)]
        public void AutoScalerWorkerReceivedCommandFromJobHost(string roleInstanceId, string deploymentId, string messageId, string senderRoleInstanceId)
        {
            if (this.IsEnabled())
                WriteEvent(908, roleInstanceId, deploymentId, messageId, senderRoleInstanceId);

        }

        [Event(909, Message = "Auto Scaler processing JobHost command: JobProcessor Role Instance: {0}, Deployment: {1}, JobHost dedicated Batch: {2}, JobHost Status Received {3}",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void AutoScalerWorkerProcessingJobHostCommand(string roleInstanceId, string deploymentId, string dedicatedBatchId, string statusReceived)
        {
            if (this.IsEnabled())
                WriteEvent(909, roleInstanceId, deploymentId, dedicatedBatchId, statusReceived);

        }

        [Event(910, Message = "Auto Scaler sending 'Run'-command to JobProcessor: JobProcessor Role Instance: {0}, Deployment: {1}, JobHost dedicated Batch: {2}, JobHost Status Received {3}",
            Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void AutoScalerWorkerSendingStartRunningCommand(string roleInstanceId, string deploymentId, string dedicatedBatchId, string statusReceived)
        {
            if (this.IsEnabled())
                WriteEvent(910, roleInstanceId, deploymentId, dedicatedBatchId, statusReceived);

        }

        [Event(911, Message = "Auto Scaler successfully sent 'Run'-command to JobProcessor: JobProcessor Role Instance: {0}, Deployment: {1}, JobHost dedicated Batch: {2}, JobHost Status Received {3}",
             Task = Tasks.AutoScaler, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void AutoScalerWorkerSuccessfullySentStartRunningCommand(string roleInstanceId, string deploymentId, string dedicatedBatchId, string statusReceived)
        {
            if (this.IsEnabled())
                WriteEvent(911, roleInstanceId, deploymentId, dedicatedBatchId, statusReceived);

        }

        [Event(912, Message = "AutoScaler failed listening to ServiceBus for receiving JobHost Updates! Role Instance: {0}, Deployment: {1} ServiceBus {2} Exception: {3} - {4}",
            Task = Tasks.JobProcessing, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void AutoScalerWorkerAutoScalerListenForJobHostUpdatesFailed(string roleInstanceId, string deploymentId, string serviceBusDetails, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(912, roleInstanceId, deploymentId, serviceBusDetails, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region Engine JobController-related Events

        [Event(1001, Message = "Job Controller - trying to submit job name {0}, job type {1}, batch {2}",
            Task = Tasks.JobController, Keywords = Keywords.Application, Level = EventLevel.LogAlways)]
        public void EngineJobControllerTrySubmittingJob(string jobName, string jobType, string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1001, jobName, jobType, batchId);
        }

        [Event(1002, Message = "Job Controller - submitting job Id {0}, job name {1}, job type {1}, batch: {2}, submitted at: {3}, scheduled by: {4}, file location: {5}, files local: {6} ",
            Task = Tasks.JobController, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobControllerSubmittingJobToQueue(string jobId, string jobName, string jobType, DateTime submittedAt, string scheduledBy, string batchId, string jobProcessFileLocation, bool areProcessFilesDeployedLocally)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1002, jobId, jobName, jobType, batchId, submittedAt, scheduledBy, jobProcessFileLocation, areProcessFilesDeployedLocally.ToString());
        }

        [Event(1003, Message = "Job Controller - submitting job id {0}, job name {1}, job type {2}, batch Id {3}, batch name {4} Failed: {5} - {6}",
            Task = Tasks.JobController, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobControllerSubmittingToBatchQueueFailed(string jobId, string jobName, string jobType, string batchId, string batchName, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(1003, jobId, jobName, jobType, batchId, batchName, exceptionMessage, exceptionStackTrace);
        }

        [Event(1004, Message = "Job Controller - submitted batch job id {0}, job name {1}, job type {2}, batch Id {3}, batch name {4}",
            Task = Tasks.JobController, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobControllerSubmittedToBatch(string jobId, string jobName, string jobType, string batchId, string batchName)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1004, jobId, jobName, jobType, batchId, batchName);
        }

        [Event(1005, Message = "Job Controller - Updating Job Repository {0}, job name {1}, job type {2} Failed {3} - {4}",
            Task = Tasks.JobController, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobControllerFailedUpdatingJob(string jobId, string jobName, string jobType, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(1005, jobId, jobName, jobType, exceptionMessage, exceptionStackTrace);
        }

        #endregion

        #region Engine JobHost-related Events

        [Event(1101, Message = "Job Host - Failed to Dequeue Message Batch Id: {0}, Batch Name {1}, Reason: {2} - {3}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobHostFailedDequeueMessage(string batchId, string batchName, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(1101, batchId, batchName, exceptionMessage, exceptionStackTrace);
        }

        [Event(1102, Message = "Job Host - Dequeue Message Count Exceeded. Job Message: {0}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Warning)]
        public void EngineJobHostMessageDequeueCountExceededError(string messageAsString)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1102, messageAsString);
        }

        [Event(1103, Message = "Job Host - Delete Message Failed. Job Message: {0}, Reason {1} - {2}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobHostFailedDeletingMessage(string messageAsString, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(1103, messageAsString, exceptionMessage, exceptionStackTrace);
        }

        [Event(1104, Message = "Job Host - Message Dequeued Successfully. Job Message: {0}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.LogAlways)]
        public void EngineJobHostReceivedMessage(string messageAsString)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1104, messageAsString);
        }

        [Event(1105, Message = "Job Host - No new messages (jobs) found to process",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobHostNoMessageAvailable()
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1105);
        }

        [Event(1106, Message = "Job Host - Deploying Job {0} for tenant {1}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobHostDeployingTenant(string jobId, string jobName, string batchId, string tenantId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1106, jobId, tenantId);
        }

        [Event(1107, Message = "Job Host - Deploying Job {0} for tenant {1} Failed: {2} - {3}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void EngineJobHostDeployingTenantFailed(string jobId, string jobName, string batchId, string tenantId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(1107, jobId, tenantId, exceptionMessage, exceptionStackTrace);
        }

        [Event(1108, Message = "Job Host - Looking-up Processor for Job Type {0}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobHostLookingUpProcessor(string jobType)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1108, jobType);
        }

        [Event(1109, Message = "Job Host - Failed to look-up Processor for Job Type {0}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void EngineJobHostProcessorLookupFailed(string jobType)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1109, jobType);
        }

        [Event(1110, Message = "Job Host - Updating Job Repository for Job Id {0}, Job Type {1} Failed: {2} - {3}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void EngineJobHostFailedUpdatingJobStatus(string jobId, string jobName, string jobType, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(1110, jobId, jobType, exceptionMessage, exceptionStackTrace);
        }

        [Event(1111, Message = "Job Host - Looking-up Job: {0} with Batch Id: {1} in Job Repository",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobHostLookingUpJobInRepository(string jobId, string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1111, jobId, batchId);
        }

        [Event(1112, Message = "Job Host - Updating Job: {0}, Batch Id: {1} with Status: {2} in Job Repository",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobHostUpdatingJobStatus(string jobId, string batchId, string newStatus)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1112, jobId, batchId, newStatus);
        }

        [Event(1113, Message = "Job Host - Job Not Found in Job Repository: Job Id {0}, Batch Id: {1}. Message will not be processed",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobHostJobForMessageNotFound(string jobId, string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1113, jobId, batchId);
        }

        [Event(1114, Message = "Job Host - Processing Job: Job Id: {0}, Job Type: {1}, Batch Id: {2}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobHostStartingJobProcessor(string jobId, string jobType, string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1114, jobId, jobType, batchId);
        }

        [Event(1115, Message = "Job Host - Processing Job: Job Id: {0}, Job Type: {1}, Batch Id: {2} Failed: {3} - {4}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobHostJobProcessorImplementationFailed(string jobId, string jobType, string batchId, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled())
                WriteEvent(1115, jobId, jobType, batchId, exceptionMessage, exceptionStackTrace);
        }

        [Event(1116, Message = "Job Host - Job Processed: Job Id: {0}, Job Type: {1}, Status: {2}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Informational)]
        public void EngineJobHostJobProcessorImplementationCompletedWithStatus(string jobId, string jobType, string jobStatus)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1116, jobId, jobType, jobStatus);
        }

        [Event(1117, Message = "Job Host - Failed sending notification message through internal ServiceBus: Role Instance: {0} Deplyoment: {1} ServiceBusTopic: {2} Subscription: {3} Exception Message: {4} Stack Trace: {5}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobHostJobProcessorFailedSendingServiceBusMessage(string roleInstanceId, string deploymentId, string serviceBusTopic, string subscription, string exceptionMessage, string exceptionStackTrace)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1117, roleInstanceId, deploymentId, serviceBusTopic, subscription, exceptionMessage, exceptionStackTrace);
        }

        [Event(1118, Message = "Job Host - Skip processing job since it is in running-stage, already: Job Id: {0}, Job Type: {1}, Batch Id: {2}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.LogAlways)]
        public void EngineJobHostSkipRunningJobSinceItIsStartedAlready(string jobId, string jobType, string batchId)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1118, jobId, jobType, batchId);
        }

        [Event(1119, Message = "Job Host - Failed setting up cancellation subscription for job: Job Id: {0}, Job Type: {1}, Batch Id: {2}, Exception Message {3}, Exception Details {4}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Critical)]
        public void EngineJobHostFailedSettingUpCancellationSubscriptionForJob(string jobId, string jobType, string batchId, string exMessage, string exDetails)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1119, jobId, jobType, batchId, exMessage, exDetails);
        }

        [Event(1120, Message = "Job Host - Failed removing/deleting cancellation subscription for job: Job Id: {0}, Job Type: {1}, Cancellation Subscription ID: {2}, Exception Message {3}, Exception Details {4}",
            Task = Tasks.JobHost, Keywords = Keywords.Application, Level = EventLevel.Error)]
        public void EngineJobHostFailedDeletingCancellationSubscriptionForJob(string jobId, string jobType, string cancellationSubscriptionId, string exMessage, string exDetails)
        {
            if (this.IsEnabled(EventLevel.Informational, Keywords.Application))
                WriteEvent(1120, jobId, jobType, cancellationSubscriptionId, exMessage, exDetails);
        }

        #endregion
    }
}