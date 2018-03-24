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
using Geres.Common.Interfaces.Engine;
using Geres.Common.Interfaces.Implementation;
using Geres.Common.Entities.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Collections.Specialized;

namespace Geres.Engine.Jobs
{
    public class JobWorkerProcessImplementation : IJobBuiltInImplementation
    {
        /// <summary>
        /// Private field holds the execution context for this job
        /// </summary>
        private BuiltInJobInitializationContext _executionContext;

        /// <summary>
        /// Using the XmlSerializer since it does not require any 3rd-party dependencies
        /// </summary>
        private XmlSerializer _jobXmlSerializer;

        /// <summary>
        /// Path to the executable for the worker process
        /// </summary>
        private string _geresWorkerProcessPath;

        /// <summary>
        /// Currently executed worker process executable
        /// </summary>
        private Process _currentWorkerProcess;

        /// <summary>
        /// Task for reading from standard output asynchronosely
        /// </summary>
        private Task _stdOutTask;

        /// <summary>
        /// Task for reading from standard error asynchronousely
        /// </summary>
        private Task _stdErrTask;

        /// <summary>
        /// Indicates, whether the job has been cancelled or not
        /// </summary>
        private bool _hasBeenCancelled;

        /// <summary>
        /// Constructor - creates all required, direct members
        /// </summary>
        public JobWorkerProcessImplementation()
        {
            _currentWorkerProcess = null;
            _stdOutTask = null;
            _stdErrTask = null;

            _jobXmlSerializer = new XmlSerializer(typeof(Common.Entities.Job));
        }

        /// <summary>
        /// Stores the execution context such as root path for the job and userName/userPassword for execution
        /// </summary>
        /// <param name="context"></param>
        public void InitializeContextBeforeExecution(BuiltInJobInitializationContext context)
        {
            _executionContext = context;
        }

        /// <summary>
        /// Calls the Geres job Worker Process executable and passes the job entity to it
        /// </summary>
        public Common.JobProcessResult DoWork(Common.Entities.Job job, string jobPackagePath, string jobWorkingPath, Action<string> progressCallback)
        {
            var errorMessages = string.Empty;
            var logMessages = string.Empty;

            //
            // At the starting point, the job has not been cancelled
            //
            _hasBeenCancelled = false;

            //
            // Parameter validations
            //
            if (_executionContext == null)
                throw new InvalidOperationException("Execution context for JobWorkerProcessImplementation is not set correctly!");

            // 
            // Copy the worker process files to the job-directory
            //
            var workerProcPath = string.Empty;
            var roleRootPath = Environment.GetEnvironmentVariable("RoleRoot");
            try
            {
                // Note: in the local emulator, RoleRoot did not have approot in it while in the cloud it did.
                if (roleRootPath[roleRootPath.Length - 1] != '\\')
                    roleRootPath += @"\";
                workerProcPath = Path.Combine(roleRootPath, "approot", "wp");
                foreach (var fileToCopy in Directory.GetFiles(workerProcPath))
                {
                    File.Copy(fileToCopy, Path.Combine(jobPackagePath, Path.GetFileName(fileToCopy)), true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to copy worker process files to job target directory. Parameters: " +
                                                  "RoleRoot={0}, JobRootPath={1}, Exception Message={2}", roleRootPath, jobPackagePath, ex.Message), ex);
            }

            //
            // First of all write the job file to the filesystem (use XML since it does not require any further dependencies)
            //
            var jobFileName = Path.Combine(jobPackagePath, JobWorkerProcessConstants.JOB_XML_FILE);
            try
            {
                using (var jobStream = new FileStream(jobFileName, FileMode.Create))
                {
                    _jobXmlSerializer.Serialize(jobStream, job);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Unable to serialize job file {0} for passing to job worker process due to exception: {1}!", job.JobId, ex.Message), ex);
            }

            //
            // Now call the executable with the username 
            //           
            try
            {
                // Compile the start info and make sure stdout and stderr are redirected
                // Note: we assume that we read from stdout, only, in our "private protocol" to reduce complexity of
                //       potenital deadlocks as described here: http://msdn.microsoft.com/en-us/library/system.diagnostics.process.standardoutput(v=vs.110).aspx 
                _geresWorkerProcessPath = Path.Combine(jobPackagePath, JobWorkerProcessConstants.JOB_WORKERPROC_EXENAME);
                var workerProcessStartInfo = new ProcessStartInfo()
                {
                    FileName = _geresWorkerProcessPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = jobWorkingPath,
                    LoadUserProfile = false,
                    CreateNoWindow = true
                };
                // Set the user name and the password if the tenant manager is configured in a way to execute the process under different credentials
                if (_executionContext.ExecutionAsUserName != null && _executionContext.ExecutionAsUserPassword != null)
                {
                    workerProcessStartInfo.UserName = _executionContext.ExecutionAsUserName;
                    workerProcessStartInfo.Password = _executionContext.ExecutionAsUserPassword;
                }
                // Had to clear environment variables since they were pointing to the "calling users" home directory
                if(workerProcessStartInfo.EnvironmentVariables.ContainsKey("homepath"))
                {
                    workerProcessStartInfo.EnvironmentVariables.Remove("homepath");
                    workerProcessStartInfo.EnvironmentVariables.Add("homepath", jobWorkingPath);
                }
                // Add environmentvariables for the job execution context (path information) for the worker process
                workerProcessStartInfo.EnvironmentVariables.Add(JobWorkerProcessConstants.JOB_ENVIRONMENT_EXEC_PATH, jobPackagePath);
                workerProcessStartInfo.EnvironmentVariables.Add(JobWorkerProcessConstants.JOB_ENVIRONMENT_WORK_PATH, jobWorkingPath);

                //
                // Now start the worker process executable
                //
                _currentWorkerProcess = Process.Start(workerProcessStartInfo);

                //
                // Create the asynchronous tasks to wait for the process, stderr, stdout
                //
                var processWaiter = Task.Run(() => _currentWorkerProcess.WaitForExit());
                _stdOutTask = Task.Run(() =>
                {
                    var msg = string.Empty;
                    do
                    {
                        msg = _currentWorkerProcess.StandardOutput.ReadLine();
                        ProcessConsoleMessage(progressCallback, msg, ref logMessages);
                    } while (!_currentWorkerProcess.HasExited);

                    do
                    {
                        msg = _currentWorkerProcess.StandardOutput.ReadLine();
                        ProcessConsoleMessage(progressCallback, msg, ref logMessages);
                    } while (!string.IsNullOrEmpty(msg));
                });
                _stdErrTask = Task.Run(() =>
                {
                    errorMessages = _currentWorkerProcess.StandardError.ReadToEnd();
                });

                //
                // Wait until stdout and stderr have completly read and the process did exit
                //
                Task.WaitAll(processWaiter, _stdOutTask, _stdErrTask);

                //
                // Compose the job output results
                //
                var outputString = logMessages;
                if (!string.IsNullOrEmpty(errorMessages))
                    outputString = string.Concat(outputString, Environment.NewLine, "--ERRORS--", Environment.NewLine, errorMessages);

                //
                // Try get the job result from the exit code
                //
                Geres.Common.Entities.JobStatus statusFromExitCode = Common.Entities.JobStatus.Finished;
                try
                {
                    statusFromExitCode = (Geres.Common.Entities.JobStatus)_currentWorkerProcess.ExitCode;
                }
                catch
                {
                    statusFromExitCode = Common.Entities.JobStatus.Aborted;
                }

                // 
                // If the job has been cancelled, the status should be cancelled
                //
                if (_hasBeenCancelled)
                    statusFromExitCode = Common.Entities.JobStatus.Cancelled;

                //
                // Finally return the result
                //
                return new Common.JobProcessResult()
                {
                    Output = outputString,
                    Status = statusFromExitCode
                };

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed calling the GERES job worker process due to the following reason: {0}!", ex.Message), ex);
            }
        }

        /// <summary>
        /// Returns the built-in job type
        /// </summary>
        public string JobType
        {
            get { return JobWorkerProcessConstants.JOB_TYPE_NAME; }
        }

        /// <summary>
        /// Asks the executable to quit and after a timeout kills it
        /// </summary>
        public void CancelProcessCallback()
        {
            if (_currentWorkerProcess != null)
            {
                if (!_currentWorkerProcess.HasExited)
                {
                    // Job has been cancelled
                    _hasBeenCancelled = true; 

                    // FUTURE FEATURE: Find a way for graceful cancellation
                    _currentWorkerProcess.Kill();
                }
            }
        }

        #region Private Helper Methods

        private static void ProcessConsoleMessage(Action<string> progressCallback, string statusLine, ref string logMessages)
        {
            if (!string.IsNullOrEmpty(statusLine))
            {
                if (!string.IsNullOrWhiteSpace(statusLine))
                {

                    if (statusLine.StartsWith(JobWorkerProcessConstants.JOB_STDOUT_PROGRESS_REPORT))
                    {
                        var progress = statusLine.Substring(JobWorkerProcessConstants.JOB_STDOUT_PROGRESS_REPORT.Length);
                        if (progressCallback != null)
                            progressCallback(progress);
                    }
                    else
                    {
                        logMessages = string.Concat(logMessages, Environment.NewLine, statusLine);
                    }
                }
            }
        }

        #endregion
    }
}
