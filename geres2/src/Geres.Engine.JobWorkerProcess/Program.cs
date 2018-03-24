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
using Geres.Common.Entities;
using Geres.Common.Interfaces.Implementation;
using Geres.Engine.JobFactories;
using Geres.Engine.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Geres.Engine.JobWorkerProcess
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                //
                // Validate parameters
                //
                var workingDirectory = Environment.GetEnvironmentVariable(JobWorkerProcessConstants.JOB_ENVIRONMENT_WORK_PATH);
                var executingDirectory = Environment.GetEnvironmentVariable(JobWorkerProcessConstants.JOB_ENVIRONMENT_EXEC_PATH);

                if (string.IsNullOrEmpty(workingDirectory))
                {
                    LogError("Missing environment variable for working directory!");
                    return (int)JobStatus.AbortedInternalError;
                }
                if (string.IsNullOrEmpty(executingDirectory))
                {
                    LogError("Missing environment variable for executing directory!");
                    return (int)JobStatus.AbortedInternalError;
                }

                //
                // Get the current working directory in which this executable has been started
                //
                Log("Worker process started");
                Log("Worker process executing directory {0}!", executingDirectory);
                Log("Worker process working directory {0}!", workingDirectory);

                //
                // Deserialize the Job-description from the filesystem which should be in the path of execution
                //
                var jobPath = Path.Combine(executingDirectory, JobWorkerProcessConstants.JOB_XML_FILE);
                Job jobToProcess = null;
                try
                {
                    using (var fs = new FileStream(jobPath, FileMode.Open, FileAccess.Read))
                    {
                        var serializer = new XmlSerializer(typeof(Job));
                        jobToProcess = (Job)serializer.Deserialize(fs);
                    }
                }
                catch(Exception ex)
                {
                    LogError(string.Format("Unable to load jobs file {0}", jobPath), ex);
                    return (int)JobStatus.AbortedInternalError;
                }

                //
                // Now resolve the job processor using the composition factory
                //
                Log("Loading job processor for job {0} with jobType {1}!", jobToProcess.JobId, jobToProcess.JobType);
                IJobImplementation jobImplementation = null;
                try
                {
                    var jobFactory = new CompositionJobFactory(executingDirectory);
                    jobImplementation = jobFactory.Lookup(jobToProcess);
                }
                catch (TypeLoadException ex)
                {
                    LogError(string.Format("Unable to load job implementation for jobId={0} with jobType={1} - TypeLoadExcpetion for type {2}!", jobToProcess.JobId, jobToProcess.JobType, ex.TypeName), ex);
                    return (int)JobStatus.AbortedJobProcessorMissingOrFailedLoading;
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    LogError(string.Format("Unable to load job implementation for jobId={0} with jobType={1} with ReflectionTypeLoadException!", jobToProcess.JobId, jobToProcess.JobType), ex);
                    foreach (var tex in ex.LoaderExceptions)
                        LogError("- Loader Exception:", tex);
                    return (int)JobStatus.AbortedJobProcessorMissingOrFailedLoading;
                }
                catch (Exception ex)
                {
                    LogError(string.Format("Unable to load job implementation for jobId={0} with jobType={1}!", jobToProcess.JobId, jobToProcess.JobType), ex);
                    return (int)JobStatus.AbortedJobProcessorMissingOrFailedLoading;
                }

                // Now execute the job itself
                Log("Start processing job...");
                try
                {
                    var jobResult = jobImplementation.DoWork
                                        (
                                            jobToProcess,
                                            executingDirectory,
                                            workingDirectory,
                                            progress =>
                                            {
                                                LogProgress(progress);
                                            }
                                        );

                    // Log that the work-implementation completed without an exception
                    Log("Job implementation completed successfully with status {0}", jobResult.Status.ToString());

                    // Log the output so that it's recorded by the system and return the status from the job execution itself
                    Log(jobResult.Output);
                    return (int)jobResult.Status;
                }
                catch (Exception ex)
                {
                    LogError("Job ran into an unhandled exception!", ex);
                    return (int)JobStatus.FailedUnexpectedly;
                }
            }
            catch (Exception ex)
            {
                LogError("An unhandled error occured in the job worker process!", ex);
                return (int)JobStatus.AbortedInternalError;
            }
        }

        #region Logging Methods

        private static void Log(string message, params string[] parameters) 
        {
            // Each ordinary Console.WriteLine will be treated as a log message
            if (parameters != null && parameters.Length > 0)
                Console.WriteLine(message, parameters);
            else
                Console.WriteLine(message);
        }

        private static void LogProgress(string progressMessage)
        {
            // Console.WriteLine with a "Progress:"-prefix will be treated as progress message
            Console.WriteLine("{0}{1}", JobWorkerProcessConstants.JOB_STDOUT_PROGRESS_REPORT, progressMessage);
        }

        private static void LogError(string errorMessage, Exception ex = null)
        {
            // Console.Error.WriteLine will be treated by the caller as error messages
            if (ex != null)
            {
                Console.Error.WriteLine("Exception occured during job processing: {0}{1}{2}", errorMessage, Environment.NewLine, ex.ToString());
                if(ex.InnerException != null)
                    LogError("Inner Exception:", ex.InnerException);
            }
            else
            {
                Console.Error.WriteLine("Error occured during job processing: {0}", errorMessage);
            }
        }

        #endregion
    }
}
