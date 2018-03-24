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
using System.Text;
using System.Threading.Tasks;

namespace Geres.Util
{
    public static class GlobalConstants
    {
        // 
        // Constants related to the overall Azure-setup
        //
        public const string PUBLISHSETTINGS_FILE_NAME = "GeRes.publishsettings";
        public const string JOBTASK_PROCESS_ROLENAME = "Geres.Azure.PaaS.JobProcessor";
        public const string LOCAL_RESOURCE_FOR_JOBS = "localjobexecutionstorage";
        public const string LOCAL_RESOURCE_FOR_TEMPJOBFILES = "localtempjobpackagestorage";
        
        //
        // Constants for general configuration settings
        // 
        public const string GERES_CONFIG_AUTOSCALER_ENABLED = "Geres.AutoScaler.EnableGeresAutoScaler";
        public const string GERES_CONFIG_AUTOSCALER_SCALE_INTERVAL = "Geres.AutoScaler.IntervalConfigInMinutes";
        public const string GERES_CONFIG_AUTOSCALER_CHECKFORJOBHOSTUPDATES_INTERVAL = "Geres.AutoScaler.JobHostUpdateCheckIntervalInSeconds";
        public const string GERES_CONFIG_JOBPROCESSOR_PAUSE_BETWEEN_WORKCHECK_SHORT = "Geres.JobProcessor.IntervalBetweenJobQueriesInMilliSeconds.Short";
        public const string GERES_CONFIG_JOBPROCESSOR_PAUSE_BETWEEN_WORKCHECK_LONG = "Geres.JobProcessor.IntervalBetweenJobQueriesInMilliSeconds.Long";
        public const string GERES_CONFIG_JOBPROCESSOR_IDLE_PING_INTERVAL = "Geres.JobProcessor.IdlePingIntervalToAutoScalerInSeconds";
        public const string GERES_CONFIG_JOBPROCESSOR_AUTOSCALER_COMMANDCHECKINTERVAL = "Geres.JobProcessor.AutoScalerCommandCheckIntervalInSeconds";

        public const string GERES_CONFIG_JOB_SINGLECANCELLATION_ENABLED = "Geres.JobProcessor.Cancellation.EnableSingleJobCancellation";
        public const string GERES_CONFIG_JOB_SINGLECANCELLATION_TIMEWINDOW_IN_SECONDS = "Geres.JobProcessor.Cancellation.SingleJobCancellationTimeWindowInSeconds";
        public const string GERES_CONFIG_JOB_SINGLECANCELLATION_MESSAGETIMETOLIVE = "Geres.JobProcessor.Cancellation.SingleJobCancellationMessageTimeToLiveInSeconds";

        public const string STORAGE_CONNECTIONSTRING_CONFIGNAME = "Geres.Storage.ConnectionString";
        public const string OTHER_WORKER_RETRYCOUNT_CONFIGNAME = "Geres.Config.WorkerAttemptsToWorkBeforeIdle";

        public const string MAX_MESSAGE_LOCK_TIME_IN_MIN = "Geres.JobProcessor.MessageLockTimeInMinutes";
        public const string MAX_MESSAGE_RETRY_ATTEMPTS = "Geres.JobProcessor.MessageRetryAttempts";
        
        //
        // Constants for Diagnostics Configuration
        //
        public const string GERES_CONFIG_DIAGNOSTICS_LEVEL = "Geres.Diagnostics.Level";
        public const string DIAGNOSTICS_STORAGE_CONNECTIONSTRING_CONFIGNAME = "Geres.Diagnostics.StorageConnectionString";

        //
        // Constants related to Azure AD
        //
        public const string AZUREAD_ADTENANT_CONFIG = "Geres.Security.AzureAdTenant";
        public const string AZUREAD_ADAUDIENCEURI_CONFIG = "Geres.Security.AzureAdAudienceUri";

        // 
        // Constants related to Azure ServiceBus
        //
        public const string SERVICEBUS_INTERNAL_CONNECTIONSTRING_CONFIGNAME = "Geres.Messaging.ServiceBusConnectionString";
        public const string SERVICEBUS_INTERNAL_TOPICS_TOPICPREFIX = "Geres2";
        public const string SERVICEBUS_INTERNAL_TOPICS_JOBSTATUS = "jobstatus";
        public const string SERVICEBUS_INTERNAL_TOPICS_CANCELJOBS = "canceljobs";
        public const string SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORJOBHOST = "commandsforjobhost";
        public const string SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORAUTOSCALER = "commandsforautoscaler";

        public const string SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBSTARTED = "jobstarted";
        public const string SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBFINISHED = "jobfinished";
        public const string SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBPROGRESS = "jobprogress";
        public const string SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBCANCELLED = "jobcancelled";
        public const string SERVICEBUS_INTERNAL_SUBSCRIPTION_JOBHOSTUPDATES = "jobhostupdates";

        //
        // Constants related to ServiceBus Messages
        //
        public const string SERVICEBUS_MESSAGE_PROP_JOBID = "JobId";
        public const string SERVICEBUS_MESSAGE_PROP_JOBNAME = "JobName";
        public const string SERVICEBUS_MESSAGE_PROP_JOBSTATUS = "Status";
        public const string SERVICEBUS_MESSAGE_PROP_PROGRESSUNIT = "ProgressUnit";
        public const string SERVICEBUS_MESSAGE_PROP_PROGRESSTOTAL = "ProgressTotal";
        public const string SERVICEBUS_MESSAGE_PROP_JOBOUTPUT = "Result";
        public const string SERVICEBUS_MESSAGE_PROP_ROLEINSTANCEID = "RoleInstanceId";

        //
        // Constants related to other SignalR-settings
        //
        public const string SIGNALR_URL = "Geres.Messaging.SignalR.HubURL";
        public const string SIGNALR_HUBNAME = "SignalRNotificationHub";
        public const string SIGNALR_METHOD_JOBSTART = "PublishJobStart";
        public const string SIGNALR_METHOD_JOBPROGRESS = "PublishJobProgress";
        public const string SIGNALR_METHOD_JOBCOMPLETED = "PublishJobComplete";


        // 
        // Job Scheduling Exceptions
        //
        public const string BATCH_DOES_NOT_EXIST_EXCEPTION = "Batch does not exist";
        public const string BATCH_IS_CLOSED_EXCEPTION = "Batch is closed";
        public const string SUBMIT_JOB_FAILED = "Submitting the Job failed";
        public const string SHARED_QUEUE_CANNOT_BE_CLOSED = "Shared queue cannot be closed";
        public const string SHARED_QUEUE_CANNOT_BE_CANCELLED = "Shared queue cannot be cancelled";

        //
        // Constants related to jobs and batches
        //
        public const string DEFAULT_BATCH_ID = "batch0";
        public const string DEFAULT_BATCH_NAME = "Default Batch";

        //
        // Retry-Policy specific constants for Storage
        //
        public const double STORAGE_RETRY_MILLISECONDS_BETWEEN_RETRY = 1000;
        public const int STORAGE_RETRY_MAX_ATTEMPTS = 5;
    }
}
