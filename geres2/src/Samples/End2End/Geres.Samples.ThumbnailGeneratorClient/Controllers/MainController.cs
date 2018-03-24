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
using Geres.ClientSdk.Core.Interfaces;
using Geres.Common.Entities;
using Geres.Samples.ThumbnailGeneratorClient.ViewModels;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Geres.Samples.ThumbnailGeneratorClient.Controllers
{
    public class MainController
    {
        public MainViewModel ViewModel { get; set; }

        private string GeresBaseUrl { get; set; }
        private string StorageConnectionString { get; set; }
        private int JobsScheduled { get; set; }
        private int JobsCompleted { get; set; }
        private string ClientId { get; set; }
        private string ClientSecret { get; set; }
        private string WindowsAzureADTenant { get; set; }
        private string WindowsAzureAdGeresWebApiId { get; set; }

        // For multi-threading to call-back into the UI-thread from a background thread
        private Dispatcher MainDispatcher = null;

        // Service client from the client SDK
        private IGeresServiceClient GeresServiceClient = null;

        /// <summary>
        /// Main constructor initializes the ViewModel and reads configuration settings.
        /// </summary>
        public MainController()
        {
            MainDispatcher = Dispatcher.CurrentDispatcher;

            ViewModel = new MainViewModel()
            {
                IsConnected = false,
                UseDefaultBatch = true,
                BatchName = "your batch name",
                SourceBlobContainerName = "source container name",
                TargetBlobContainerName = "target container name",
                AllJobsCompleted = true,
                ClientLogActions = new ObservableCollection<string>(),
                NotificationMessages = new ObservableCollection<string>()
            };

            // Write the first log message into the client app log
            ViewModel.ClientLogActions.Add("Welcome to client app!");

            // Read data from the app.config configuration setting file...
            ReadConfigurationValues();

            // Create the Geres Service Client from the SDK
            GeresServiceClient = new Geres.ClientSdk.NetFx.GeresAzureAdServiceClient
                                                            (
                                                                GeresBaseUrl,
                                                                WindowsAzureADTenant,
                                                                WindowsAzureAdGeresWebApiId
                                                            );
        }

        /// <summary>
        /// Connects to the SignalR notifications channel
        /// </summary>
        public async Task Connect()
        {
            try
            {
                // First of all authenticate
                AddNotificationMessage("Authenticating against Windows Azure AD...", true);
                var authenticated = await GeresServiceClient.Authenticate(ClientId, ClientSecret);
                if (authenticated)
                {
                    AddNotificationMessage("Authenticated successfully against AD!", true);
                }
                else
                {
                    AddNotificationMessage("Authentication failed with no exception!", true);
                    ViewModel.IsConnected = false;
                    return;
                }

                // Now connect for notifications
                AddNotificationMessage(string.Format("Connecting for notifications..."), true);
                GeresServiceClient.Notifications.StatusChanged += (sender, args) =>
                {
                    AddNotificationMessage(
                        string.Format("Notification status changing from {0} to {1}...", args.OldStatus, args.NewStatus), 
                        false);
                };
                GeresServiceClient.Notifications.ExceptionOccured += (sender, args) =>
                {
                    AddNotificationMessage
                        (
                            string.Format("Exception occured on notification hub: {0}", args.Message),
                            false
                        );
                };
                GeresServiceClient.Notifications.JobStarted += (sender, args) =>
                {
                    AddNotificationMessage(
                        string.Format("Started job with id {0}, name {1} and the following parameters:", 
                                    args.JobDetails.JobId, args.JobDetails.JobName), false);
                    AddNotificationMessage(string.Format("     {0}", args.JobDetails.Parameters), false);
                };
                GeresServiceClient.Notifications.JobCompleted += (sender, args) =>
                {
                    JobsCompleted++;
                    AddNotificationMessage(string.Format("Completed job with id {0} and name {1}!!", args.JobDetails.JobId, args.JobDetails.JobName), false);

                    if (JobsCompleted == JobsScheduled)
                    {
                        ViewModel.AllJobsCompleted = true;
                        AddNotificationMessage("-------", false);
                        AddNotificationMessage("All scheduled work is done!", false);
                        AddNotificationMessage("-------", false);
                    }
                };
                await GeresServiceClient.Notifications.Connect();

                // Connected and ready to schedule jobs
                ViewModel.IsConnected = true;
            }
            catch (Exception ex)
            {
                AddNotificationMessage(string.Format("Authentication or connecting to notification hub failed: {0}", ex.Message), true);
                ViewModel.IsConnected = false;
            }
        }

        /// <summary>
        /// Reads the images from the source container and schedules a job for each image in a new batch
        /// </summary>
        public async void ScheduleJobs()
        {
            //
            // Get the images from the source blob container
            //
            AddNotificationMessage(string.Format("Getting images from BLOB-container {0}...", ViewModel.SourceBlobContainerName), true);
            string[] imageNames = await GetImagesFromSourceContainer();

            //
            // Create a batch if needed
            //
            var batchId = await CreateBatchForJobs();

            //
            // Now for each image schedule a job
            //
            AddNotificationMessage(string.Format("Scheduling jobs for {0} number of images...", imageNames.Length), true);
            foreach (var image in imageNames)
            {
                await ScheduleJobForImage(image, batchId);
            }
        }

        #region Job Scheduling Helpers

        private async Task ScheduleJobForImage(string image, string batchId)
        {
            //
            // Compose the parameters string for the job
            //
            var jobParameters = string.Format
                                (
                                    "{0}|{1}|{2}|{3}", 
                                    StorageConnectionString,
                                    ViewModel.SourceBlobContainerName,
                                    image, 
                                    ViewModel.TargetBlobContainerName
                                );

            //
            // After the batch has been created (if any other batch than the default one is used), schedule the job
            //
            AddNotificationMessage("Submitting job for image " + image, true);
            var jobId = await GeresServiceClient.Management.SubmitJob
                                (
                                    new Job()
                                        {
                                            JobName = string.Format("Converting image {0}", image),
                                            JobType = "ThumbnailJob",
                                            Parameters = jobParameters,
                                            JobProcessorPackageName = "Geres.Samples.ThumbnailGeneratorJob.zip",
                                            TenantName = "ThumbnailTenant"
                                        },
                                    batchId
                                );
            AddNotificationMessage(string.Format("     Job with id {0} accepted!", jobId), true);

            //
            // (Optional) Subscribe on SignalR for real-time status updates on the job
            //
            AddNotificationMessage(string.Format("     Subscribing for updates on job {0}!", jobId), true);
            GeresServiceClient.Notifications.SubscribeToJob(jobId);
            AddNotificationMessage(string.Format("     Successfully subscribed on job {0}!", jobId), true);
        }

        private async Task<string> CreateBatchForJobs()
        {
            //
            // Compose the URLs for the WebAPIs
            //
            var batchId = string.Empty;

            //
            // Create the batch if a custom batch should be used
            //
            if (ViewModel.UseCustomBatch)
            {
                var newBatch = new Batch()
                {
                    BatchName = ViewModel.BatchName,
                    Priority = 0
                };

                // Create the batch using the service client
                batchId = await GeresServiceClient.Management.CreateBatch(newBatch);
            }

            return batchId;
        }

        #endregion

        #region Private Helper Methods

        private void ReadConfigurationValues()
        {
            AddNotificationMessage("Reading configuration setting 'GeresBaseUrl'...", true);
            GeresBaseUrl = ConfigurationManager.AppSettings["GeresBaseUrl"];
            AddNotificationMessage(string.Format("  Using the following GeresBaseUrl: {0}", GeresBaseUrl), true);

            AddNotificationMessage("Reading configuration setting 'StorageConnectionString'...", true);
            StorageConnectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
            AddNotificationMessage(string.Format("  Using the following StorageConnectionString: {0}", StorageConnectionString), true);

            AddNotificationMessage("Reading authentication settings...", true);
            ClientId = ConfigurationManager.AppSettings["ClientId"];
            ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            WindowsAzureADTenant = ConfigurationManager.AppSettings["WindowsAzureADTenant"];
            WindowsAzureAdGeresWebApiId = ConfigurationManager.AppSettings["WindowsAzureAdGeresWebApiId"];
            AddNotificationMessage(string.Format("  Using Windows Azure AD {0} for WebAPI {1} with client ID {2} and configured client secrent in App.config!", WindowsAzureADTenant, WindowsAzureAdGeresWebApiId, ClientId), true);
        }

        private async Task<string[]> GetImagesFromSourceContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(ViewModel.SourceBlobContainerName);
            if (!blobContainer.Exists())
                throw new Exception("Blob storage container with source images does not exist!");

            var token = new BlobContinuationToken();
            var imagesList = new List<string>();
            do
            {
                var blobsInContainer = await blobContainer.ListBlobsSegmentedAsync(token);
                foreach (var blob in blobsInContainer.Results)
                {
                    var blobRef = await blobContainer.GetBlobReferenceFromServerAsync(blob.Uri.ToString());
                    imagesList.Add(blobRef.Name);
                }
                token = blobsInContainer.ContinuationToken;
            } while (token != null);

            return imagesList.ToArray();
        }

        private void AddNotificationMessage(string message, bool isClient)
        {
            MainDispatcher.Invoke(() =>
            {
                string finalMsg = string.Format("{0} -- {1}", DateTime.Now.ToShortTimeString(), message);
                if (isClient)
                    ViewModel.ClientLogActions.Add(finalMsg);
                else
                    ViewModel.NotificationMessages.Add(finalMsg);
            });
        }

        #endregion
    }
}
