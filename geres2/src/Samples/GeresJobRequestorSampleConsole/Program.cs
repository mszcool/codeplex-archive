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
using System.Net;
using System.Net.Http;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geres.Common.Entities;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Configuration;
using Geres.ClientSdk.NetFx;

namespace JobRequestor_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Program();
            p.Run();
            Console.Read();
        }

        private void Run()
        {
            var batchId = string.Empty;
            var batchPriority = 999;
            var completedJobs = 0;
            var jobType = string.Empty;
            var baseUrl = string.Empty;
            var currentConsoleColor = Console.ForegroundColor;

            #region User Input

            // ****************************
            // Ask the user for their input
            // ****************************


            string submitJobsOrGetJobs = string.Empty;
            string tenantId = string.Empty;
            string jobTypeToRun = string.Empty;
            string batchOrSingle = string.Empty;
            string runLocal = string.Empty;
            int jobsToRun = 0;
            string jobOrBatchIdToCancel = string.Empty;

            Console.WriteLine("[G]et jobs and batches\r\n[S]ubmit jobs\r\n[C]ancel Jobs\r\n[K]ill batch by cancellation");
            Console.Write("Enter option: ");
            submitJobsOrGetJobs = Console.ReadLine();

            if (submitJobsOrGetJobs.ToLower().Equals("s"))
            {
                Console.Write("Please enter your TenantId: ");
                tenantId = Console.ReadLine();

                Console.Write("Type of job to run: ([F]inancialYearEnd or [U]pdateWaitPostings)? ");
                jobTypeToRun = Console.ReadLine();
                if (jobTypeToRun.ToLower().Equals("u"))
                    jobType = "UpdateWaitingPostings";
                else
                    jobType = "FinancialYearEnd";

                Console.Write("[B]atch or [S]ingle? ");
                batchOrSingle = Console.ReadLine();

                if (batchOrSingle.Equals("b", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.Write("Please enter the Priority of the batch as an integer from 1-998 ");
                    batchPriority = int.Parse(Console.ReadLine());
                }

                Console.Write("How many jobs would you like to run? ");
                jobsToRun = int.Parse(Console.ReadLine());
            }

            Console.Write("Do you want to run local (y/n)? ");
            runLocal = Console.ReadLine();
            if (runLocal.ToLower().Equals("y"))
                baseUrl = ConfigurationManager.AppSettings["GeresBaseUrl_Local"];
            else
                baseUrl = ConfigurationManager.AppSettings["GeresBaseUrl_Cloud"];

            // *****************************

            #endregion

            #region Setup the Geres2 service client

            Console.WriteLine();

            var azureAdTenant = ConfigurationManager.AppSettings["WindowsAzureADTenant"];
            var clientId = ConfigurationManager.AppSettings["ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
            var azureAdWebApiId = ConfigurationManager.AppSettings["WindowsAzureAdGeresWebApiId"];

            var geresServiceClient = new GeresAzureAdServiceClient
                                        (
                                            baseUrl,
                                            azureAdTenant,
                                            azureAdWebApiId
                                        );

            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Authenticating against Windows Azure AD...");
                geresServiceClient.Authenticate(clientId, clientSecret).Wait();
                Console.WriteLine("Authenticated successfully!");
                Console.ForegroundColor = currentConsoleColor;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Authentication failed: {0}, press ENTER to stop!" + ex.Message);
                Console.ReadLine();
                return;
            }

            #endregion

            if (submitJobsOrGetJobs.ToLower().Equals("s"))
            {
                #region Notification Handlers

                Console.WriteLine();
                Console.ForegroundColor = currentConsoleColor;
                Console.WriteLine("Connecting to notification hub...");

                // Connection status changes
                geresServiceClient.Notifications.StatusChanged += (sender, args) =>
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Notification connection status change from {0} to {1}", args.OldStatus, args.NewStatus);
                    Console.ForegroundColor = currentConsoleColor;
                };

                // Connection exceptions
                geresServiceClient.Notifications.ExceptionOccured += (sender, ex) =>
                {
                    System.Diagnostics.Debug.WriteLine("Notification hub exception occured: {0}", ex.Message);
                };

                // Job Starts
                geresServiceClient.Notifications.JobStarted += (sender, args) =>
                {
                    Console.WriteLine("Job {0} --> Processing", args.JobDetails.JobId);
                };

                // Job Progress
                geresServiceClient.Notifications.JobProgressed += (sender, args) =>
                {
                    var progress = args.ProgressDetails;
                    Console.WriteLine("Job {1} --> {0}%", progress, args.JobDetails.JobId);
                };

                // Job Complete
                geresServiceClient.Notifications.JobCompleted += (sender, args) =>
                {
                    var prevConsoleColor = Console.ForegroundColor;
                    if (args.JobDetails.Status == JobStatus.Finished)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Job {0} --> {1}", args.JobDetails.JobId, args.JobDetails.Status);
                    Console.ForegroundColor = prevConsoleColor;

                    completedJobs++;

                    if (completedJobs == jobsToRun)
                    {
                        if (!string.IsNullOrEmpty(batchId))
                        {
                            // close the batch
                            geresServiceClient.Management.CloseBatch(batchId);
                        }

                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine();
                        Console.WriteLine("All Jobs Complete");
                        Console.ForegroundColor = currentConsoleColor;
                    }
                };

                geresServiceClient.Notifications.Connect().Wait();
                Console.WriteLine("Connected to notification hub!");

                #endregion

                #region Job Creation

                if (batchOrSingle.Equals("B", StringComparison.CurrentCultureIgnoreCase))
                {
                    var batch = new Batch()
                    {
                        BatchName = "test_" + DateTime.UtcNow.ToString(),
                        Priority = batchPriority
                    };

                    // create the batch
                    try
                    {
                        batchId = geresServiceClient.Management.CreateBatch(batch).Result;
                    }
                    catch (HttpClientException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Creating a batch failed {0}!", ex.Message);
                        Console.ForegroundColor = currentConsoleColor;
                        return;
                    }
                }

                for (int i = 1; i <= jobsToRun; i++)
                {
                    // create a new job
                    var newJob = new Job
                    {
                        JobType = jobType,
                        JobName = string.Format("{0} {1}", jobType, i),
                        TenantName = tenantId,
                        JobProcessorPackageName = "geresjobsamples.zip",
                        Parameters = String.Empty
                    };

                    try
                    {
                        string jobId = geresServiceClient.Management.SubmitJob(newJob, batchId).Result;
                        try
                        {
                            geresServiceClient.Notifications.SubscribeToJob(jobId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed subscribing to job: {0}!", ex.Message);
                        }
                        Console.WriteLine("Job {0} : {1} --> Submitted", i, jobId);

                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Failed submitting job: {0}", ex.Message);
                        Console.ForegroundColor = currentConsoleColor;
                    }
                }

                // If Cancel wants to tested then uncomment the following lines
                // System.Threading.Thread.Sleep(10000);
                //geresServiceClient.Management.CancelJob(jobId).Wait();

                #endregion
            }
            else if(submitJobsOrGetJobs.ToLower().Equals("g"))
            {
                #region Get batches and jobs through monitoring API

                Console.WriteLine();
                Console.WriteLine("Loading batches from monitoring API...");
                var batchesTask = geresServiceClient.Monitoring.GetBatches();
                batchesTask.Wait();
                Console.WriteLine("Found batches:");
                foreach (var oneBatch in batchesTask.Result)
                {
                    Console.WriteLine("Batch found:");
                    Console.WriteLine("\tBatchId\t{0}\r\n\tBatchName\t{1}", oneBatch.Id, oneBatch.BatchName);
                }
                Console.WriteLine();
                Console.Write("Enter Batch ID to query for next: ");
                var batchIdToGetJobs = Console.ReadLine();
                Console.Write("How many jobs to request at one request: ");
                var pageSize = int.Parse(Console.ReadLine());
                Console.WriteLine("*************************************");

                do
                {
                    Console.WriteLine();
                    Console.WriteLine("Loading jobs from batch {0} in list...", batchIdToGetJobs);
                    geresServiceClient.Monitoring.PageSize = pageSize;
                    var jobsTask = geresServiceClient.Monitoring.GetJobsFromBatch(batchIdToGetJobs);
                    jobsTask.Wait();
                    Console.WriteLine("Found jobs:");
                    foreach (var j in jobsTask.Result)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Try reading job details...");

                        #region Demonstrate the method to read a single job

                        Task<Job> jobDetailsReadAgainToTestSingleJobCallMethod = null;
                        if (batchIdToGetJobs == "batch0")
                            jobDetailsReadAgainToTestSingleJobCallMethod = geresServiceClient.Monitoring.GetJobDetails(j.JobId);
                        else
                            jobDetailsReadAgainToTestSingleJobCallMethod = geresServiceClient.Monitoring.GetJobDetails(j.JobId, batchIdToGetJobs);

                        try
                        {
                            jobDetailsReadAgainToTestSingleJobCallMethod.Wait();
                        }
                        catch (Exception ex)
                        {
                            var currentConsoleColorJobDetailsRead = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed reading details for single job {0} via Web API!", j.JobId);
                            Console.WriteLine("Error: {0}\r\n{1}", ex.Message, ex.ToString());
                            Console.ForegroundColor = currentConsoleColorJobDetailsRead;
                        }

                        #endregion

                        Console.WriteLine();
                        Console.WriteLine("Job Details:");
                        Console.WriteLine("ID\t\t{0}\r\ntype\t\t{1}\r\nstatus\t\t{2}\r\n\r\nOutput:\r\n\r{3}\r\n", j.JobId, j.JobType, j.Status, j.Output);
                        Console.WriteLine("----------------------------");
                    }

                    if (!string.IsNullOrEmpty(geresServiceClient.Monitoring.CurrentPagingToken))
                    {
                        Console.WriteLine("Press ENTER to load next set of jobs...");
                        Console.ReadLine();
                    }
                } while (!string.IsNullOrEmpty(geresServiceClient.Monitoring.CurrentPagingToken));

                #endregion
            }
            else if (submitJobsOrGetJobs.ToLower().Equals("c"))
            {
                #region Cancellation of a single job

                Console.Write("Please enter the ID of the job to be cancelled: ");
                jobOrBatchIdToCancel = Console.ReadLine();

                geresServiceClient.Management.CancelJob(jobOrBatchIdToCancel);

                Console.WriteLine("Job cancellation request sent!");

                #endregion
            }
            else if (submitJobsOrGetJobs.ToLower().Equals("k"))
            {
                #region Cancellation of a complete batch

                Console.WriteLine("Please enter the ID of the batch to be cancelled: ");
                jobOrBatchIdToCancel = Console.ReadLine();

                geresServiceClient.Management.CancelBatch(jobOrBatchIdToCancel);

                Console.WriteLine("Batch cancellation request sent!");

                #endregion
            }
            else
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid operation selected, closing application without action!");
                Console.ForegroundColor = currentConsoleColor;
            }
        }
    }
}
