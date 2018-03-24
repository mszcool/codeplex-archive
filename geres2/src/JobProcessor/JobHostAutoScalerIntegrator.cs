using Geres.Common.Entities;
using Geres.Diagnostics;
using Geres.Util;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Azure.PaaS.JobProcessor
{
    public class JobHostAutoScalerIntegrator
    {
        // Attributes received at construction
        private bool _isEnabled;
        private string _serviceBusConnectionString;

        // Attributes received at initialization
        private string _roleInstanceId;
        private string _deploymentId;
        private string _jobHostId;
        private int _autoScalerCommandCheckIntervalInSeconds;
        private int _retryCountBeforeIdle;
        private int _idlePingIntervalInSeconds;

        // Private members not received from outside
        private string _subscriptionName;
        private JobHostServiceBus _jobHostServiceBus;
        private int _currentRetryCount;
        private bool _workerIsIdle;
        private DateTime _lastTimeIdleSent;
        private string _dedicatedBatchId;


        public JobHostAutoScalerIntegrator(bool isEnabled, string serviceBusConnectionString)
        {
            // Store the settings as member variables
            _isEnabled = isEnabled;
            _serviceBusConnectionString = serviceBusConnectionString;
        }

        public string AssignedDedicatedBatchId
        {
            get { return _dedicatedBatchId; }
        }

        public void Initialize(string roleInstanceId, string deploymentId, int autoScalerCommandCheckIntervalInSeconds, int retryCountBeforeIdle, int idlePingIntervalInSeconds)
        {
            // Do not do anything if the AutoScaler should not be enabled
            if (!_isEnabled) return;

            // Log that the AutoScaler is initalized
            GeresEventSource.Log.JobProcessorWorkerInitializingAutoScalerServiceBus(roleInstanceId, deploymentId);

            // 
            // Store the values in the implementation
            //
            _roleInstanceId = roleInstanceId;
            _deploymentId = deploymentId;
            _autoScalerCommandCheckIntervalInSeconds = autoScalerCommandCheckIntervalInSeconds;
            _retryCountBeforeIdle = retryCountBeforeIdle;
            _idlePingIntervalInSeconds = idlePingIntervalInSeconds;

            //
            // Set Default values for the setup
            //
            _workerIsIdle = true;   // Worker starts "ready" and waiting for AutoScaler to unblock!
            _lastTimeIdleSent = DateTime.UtcNow;
            _currentRetryCount = _retryCountBeforeIdle;
            _dedicatedBatchId = string.Empty;

            //
            // Generate a valid jobHostId for the AutoScaler Worker Status Table
            //
            if (RoleEnvironment.IsEmulated)
            {
                // get the last section _IN_x
                var instanceNumberStart = _roleInstanceId.IndexOf("_IN_");

                // attach it to the user name to avoid "race conditions" between multiple instances simulated on the same machine
                _jobHostId = string.Concat(
                    RoleEnvironment.CurrentRoleInstance.Role.Name,
                    _roleInstanceId.Substring(instanceNumberStart)
                 );
            }
            else
            {
                _jobHostId = _roleInstanceId;
            }

            //
            // Create the JobHostServiceBus used for talking to the service bus for AutoScaler
            //
            _jobHostServiceBus = new JobHostServiceBus(
                                        _serviceBusConnectionString,
                                        GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORAUTOSCALER,
                                        GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORJOBHOST);
            _subscriptionName = _jobHostId;

            //
            // Subscribe to the AutoScaler commands topic
            //
            if (_isEnabled)
            {
                Task.Run(() =>
                {
                    AutoScalerReceiveLoop();
                });
            }

            //
            // Send a message to the Autoscaler that the worker is ready
            //
            GeresEventSource.Log.JobProcessorWorkerSendingInstanceReadyToAutoScaler(_roleInstanceId, _deploymentId);
            SendAutoScaleReadyStatusMessage();
            GeresEventSource.Log.JobProcessorWorkerSendingInstanceReadyToAutoScalerCompleted(_roleInstanceId, _deploymentId);

            // Log that initialization went well
            GeresEventSource.Log.JobProcessorWorkerInitializingAutoScalerServiceBusCompleted(_roleInstanceId, _deploymentId, _subscriptionName);
        }

        public bool VerifyIfWorkerShouldBeIdle()
        {
            // AutoScaler is not enabled, Worker should never go IDLE
            if (!_isEnabled) return false;

            // Notify the worker is checking whether it's idle or not
            GeresEventSource.Log.JobProcessorWorkerWaitingForUnblockResetEvent(_roleInstanceId, _deploymentId);

            // Verify, if the worker should be IDLE or not
            if (_workerIsIdle)
            {
                // For being more failure resilient, send a message every n seconds that we are IDLE so the AutoScaler can cross-check
                if (DateTime.UtcNow.Subtract(_lastTimeIdleSent).TotalSeconds > _idlePingIntervalInSeconds)
                {
                    Geres.Diagnostics.GeresEventSource.Log.JobProcessorWorkerAutoScalerNotifyIdleStatus(_roleInstanceId, _deploymentId);
                    SendAutoScaleIdleStatusMessage();
                }

                // Continue with a non-working while-loop since the worker is IDLE
                return true;
            }

            // Worker is not Idle, return false
            return false;
        }

        public void RegisterRetryProcessing()
        {
            // Never go Idle if AutoScaler-Integration is not enabled
            if (!_isEnabled) return;

            // Update the number of retries before going Idle
            _currentRetryCount--;

            if (_currentRetryCount == 0)
            {
                // If the Number of retries is 0, then notify that this worker is Idle
                GeresEventSource.Log.JobProcessorWorkerAutoScalerNotifyIdleStatus(_roleInstanceId, _deploymentId);

                // send a signal to block this thread so that the worker does not try to
                // process anymore jobs
                GeresEventSource.Log.JobProcessorWorkerBlockUntilAutoScalerRelease(_roleInstanceId, _deploymentId);
                _workerIsIdle = true;

                // reset the number of retries - this is in finally in case any error was
                // caught above the loop will continue to run
                _currentRetryCount = _retryCountBeforeIdle;

                // the number of retries before shutdown has been exceeded
                // this worker has had no jobs for the max number of retries / wait time
                // tell the autoscaler that this worker is now idle
                SendAutoScaleIdleStatusMessage();
            }
        }

        public void StopAutoScaleInteraction()
        {
            if (_isEnabled)
            {
                GeresEventSource.Log.JobProcessorWorkerRemovingAutoScalerCommandSubscription(_roleInstanceId, _deploymentId, _subscriptionName);
                _jobHostServiceBus.DeleteSubscription(_subscriptionName);
            }
        }

        #region Private Implementation Methods

        private void SendAutoScaleReadyStatusMessage()
        {
            if (!_isEnabled) return;

            GeresEventSource.Log.JobProcessorWorkerSendingInstanceReadyToAutoScaler(_roleInstanceId, _deploymentId);
            _jobHostServiceBus.SendAutoScaleUpdateMessage(new JobHost
            {
                Status = JobHostStatus.Ready,
                RoleInstanceId = _roleInstanceId,
                Id = _jobHostId,
                DeploymentId = _deploymentId,
                DedicatedBatchId = _dedicatedBatchId
            });
            GeresEventSource.Log.JobProcessorWorkerSendingInstanceReadyToAutoScalerCompleted(_roleInstanceId, _deploymentId);
        }

        private void SendAutoScaleIdleStatusMessage()
        {
            if (!_isEnabled) return;

            Geres.Diagnostics.GeresEventSource.Log.JobProcessorWorkerAutoScalerNotifyIdleStatus(_roleInstanceId, _roleInstanceId);
            _jobHostServiceBus.SendAutoScaleUpdateMessage(new JobHost
            {
                Status = JobHostStatus.Idle,
                RoleInstanceId = _roleInstanceId,
                Id = _jobHostId,
                DeploymentId = _deploymentId,
                DedicatedBatchId = _dedicatedBatchId
            });

            // Store when the IDLE message was sent last time
            _lastTimeIdleSent = DateTime.UtcNow;
        }

        private void AutoScalerReceiveLoop()
        {
            SubscriptionClient client = null;
            while (true)
            {
                try
                {
                    if (client == null || client.IsClosed)
                        client = _jobHostServiceBus.CreateSubscription(_subscriptionName, _roleInstanceId);

                    var msg = client.Receive(TimeSpan.FromSeconds(_autoScalerCommandCheckIntervalInSeconds));
                    if (msg != null)
                    {
                        if (ProcessAutoScalerCommand(msg))
                            msg.Complete();
                        else
                            msg.Abandon();
                    }
                }
                catch (Exception ex)
                {
                    client = null;
                    Geres.Diagnostics.GeresEventSource.Log.AutoScalerWorkerAutoScalerListenForJobHostUpdatesFailed
                        (
                            _roleInstanceId, _deploymentId,
                            string.Format("Topic: {0}, Subscription: {1}", _subscriptionName, GlobalConstants.SERVICEBUS_INTERNAL_TOPICS_COMMANDSFORJOBHOST),
                            ex.Message,
                            ex.StackTrace
                        );
                }
            }
        }

        private bool ProcessAutoScalerCommand(BrokeredMessage msg)
        {
            GeresEventSource.Log.JobProcessorWorkerAutoScalerCommandReceived(_roleInstanceId, _deploymentId);

            try
            {
                var jobHost = msg.GetBody<JobHost>();

                switch (jobHost.Status)
                {
                    case JobHostStatus.Run:
                        {
                            // Log that we actually received a running-command
                            GeresEventSource.Log.JobProcessorWorkerReceivedAutoScalerStartRunning(_roleInstanceId, _deploymentId);

                            // unblock the worker while loop thread
                            _workerIsIdle = false;
                        }

                        break;
                }

                // should this host look a specific queue / batch
                _dedicatedBatchId = jobHost.DedicatedBatchId;

                // Processed successfully
                return true;
            }
            catch (Exception ex)
            {
                GeresEventSource.Log.JobProcessorWorkerAutoScalerCommandReceivedProcessingError(_roleInstanceId, _deploymentId, ex.Message, ex.StackTrace);
                return false;
            }
        }

        #endregion
    }
}
