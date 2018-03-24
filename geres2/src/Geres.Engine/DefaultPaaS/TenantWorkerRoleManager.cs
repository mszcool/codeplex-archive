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
using Geres.Common.Interfaces.Engine;
using Geres.Common.Entities.Engine;
using Geres.Util;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Web;
using System.Web.Security;
using System.Security.AccessControl;

namespace Geres.Engine.DefaultPaaS
{
    public class TenantManager : ITenantManager
    {
        private string storageConn;
        private string applicationsRootPath;
        private string processorFilesRootPath;
        private string temporaryFilesRootPath;

        // Cache for tenant users used for job execution across all threads in the worker role host
        private static Dictionary<string, byte[]> inMemoryPasswordCacheForThisHost = new Dictionary<string, byte[]>();

        // DPAPI protection of in-memory held passwords
        DpapiDataProtector inMemoryPasswordProtector = null;

        private const string DPAPI_APPNAME = "geresjobhosttenantmanager";
        private const string DPAPI_PURPOSENAME = "geresjobhostpasswordencryptionforexecution";

        private const int PASSWORD_LENGTH = 32;
        private const int PASSWORD_NONALPHANUMERICS = 16;
        private const string USER_GROUP_NAME = "JobProcessors";
        private const string USERNAME_PREFIX = "g";
        private const string CONTAINERNAME = "jobprocessorfiles";
        private const string WORK_PATH_NAME = "work";

        /// <summary>
        /// Initializes the tenant manager instance
        /// </summary>
        public void Initialize(string baseDirectory, string baseTempDirectory)
        {
            inMemoryPasswordProtector = new DpapiDataProtector(DPAPI_APPNAME, DPAPI_PURPOSENAME);

            storageConn = CloudConfigurationManager.GetSetting(GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME);
            if (string.IsNullOrEmpty(storageConn.Trim()))
                throw new Exception("Missing configuration setting " + GlobalConstants.STORAGE_CONNECTIONSTRING_CONFIGNAME);

            applicationsRootPath = Path.Combine(baseDirectory, "apps");
            processorFilesRootPath = Path.Combine(baseDirectory, "procs");
            temporaryFilesRootPath = Path.Combine(baseTempDirectory, "tmp");
        }

        /// <summary>
        /// Create a new user
        /// Create a new folder for this job
        /// Copy the job process files for this job
        /// </summary>
        /// <param name="job"></param>
        /// <param name="storageConn"></param>
        public TenantJobExecutionContext DeployTenant(Job job, string batchId)
        {
            var userName = USERNAME_PREFIX + job.TenantName;
            if (RoleEnvironment.IsEmulated)
            {
                // In case we run in the emulator we need to apply a simple strategy to avoid running into race conditions between multiple, simulated instances on the same machine in the emulator
                var roleInstanceId = RoleEnvironment.CurrentRoleInstance.Id;

                // get the last section _IN_x
                var instanceNumberStart = roleInstanceId.IndexOf("_IN_");

                // attach it to the user name to avoid "race conditions" between multiple instances simulated on the same machine
                userName = string.Concat(userName, roleInstanceId.Substring(instanceNumberStart));
                userName = userName.Replace("_", "");
            }

            // e.g. c:\applications\<tenantid>\<batchid>\<jobid>\
            string appsDestinationPath = Path.Combine(applicationsRootPath, userName, batchId, job.JobId);

            if (Directory.Exists(appsDestinationPath) == false)
            {
                // create a new folder for this tenant\batch\jobid
                Directory.CreateDirectory(appsDestinationPath);
            }

            // create the working sub directory to which the job will have write permissions
            string workingSubPath = Path.Combine(appsDestinationPath, WORK_PATH_NAME);
            if(!Directory.Exists(workingSubPath))
                Directory.CreateDirectory(workingSubPath);

            // check if the files are already downloaded
            string processorFilesPath = Path.Combine(processorFilesRootPath, userName, job.JobProcessorPackageName);

            // have the files already been downloaded to the processor files path
            // if so, then reuse these files.
            if (Directory.Exists(processorFilesPath) == false)
            {
                Directory.CreateDirectory(processorFilesPath);

                // what is the relative address of the file images
                string blobDirectoryAddress = job.JobProcessorPackageName;

                // download the blob and then unpack
                // create a temporary file to download the zip file to:
                string tempfilename = Path.Combine(temporaryFilesRootPath, string.Format("{0}.zip", Guid.NewGuid().ToString()));

                // download the file
                DownloadBlob(blobDirectoryAddress, tempfilename, storageConn, job.TenantName);

                // extract the files to the processor files path...this means the files can be downloaded once and reused
                ZipFile.ExtractToDirectory(tempfilename, processorFilesPath);

                // once extracted remove the temp file
                File.Delete(tempfilename);
            }

            // in any case copy the job executables previously downloaded to the execution directory
            CopyDirectory(processorFilesPath, appsDestinationPath);

            // FUTURE FEATURE: one of the next versions will allow the execution of jobs under a different user. This requires to update
            // FUTURE FEATURE: the JobWorkerProcessImplememtation in Geres.Engine.BuiltInJobs which is prepared to run a process under a different user
            // FUTURE FEATURE: but it also requires changing the security status of the worker which today runs elevated and since calling a non-elevated process
            // FUTURE FEATURE: from an elevated process (worker host calling JobWorkerProcess) is not allowed in Windows. This is a bigger refactoring since it 
            // FUTURE FEATURE: means the TenantManager needs to be extracted into an external, elevated process for this purpose.
            // create an identity for the batch process.
            SecureString userPassword = null;
            /* 
             * FUTURE FEATURE BEGIN
                var userPassword = TryCreateNewUser(userName);
                if (userPassword == null)
                    throw new Exception("Failed deploying new tenant - unable to determine, set or retrieve password for the user for tenant-execution!");
                // assign the correct user permissions to the folder
                AddUserPermissionsToFolder(appsDestinationPath, userName, FileAccessPermission.ReadExecute);
                AddUserPermissionsToFolder(workingSubPath, userName, FileAccessPermission.Full);
             * 
             * FUTURE FEATURE END 
            */

            // return details on the execution context
            if (!string.IsNullOrEmpty(appsDestinationPath.Trim()))
            {
                return new TenantJobExecutionContext()
                {
                    JobRootPath = appsDestinationPath,
                    JobWorkingRootPath = workingSubPath,
                    UserName = userName,
                    UserPassword = userPassword
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Happens when the machine goes idle and is a useful tidy-up piece
        /// </summary>
        public void DeleteTenants()
        {
            try
            {
                DeleteDirectory(applicationsRootPath);
            }
            catch { }

            try
            {
                DeleteDirectory(processorFilesRootPath);
            }
            catch { }

            try
            {
                DeleteDirectory(temporaryFilesRootPath);
            }
            catch { }

            try
            {
                DeleteUsers();
            }
            catch { }
        }

        /// <summary>
        /// When a job completes, remove the job directory within the tenant's folder
        /// </summary>
        /// <param name="job"></param>
        /// <param name="storageConn"></param>
        /// <param name="batchId"></param>
        public void DeleteJobDirectory(Job job, string batchId)
        {
            try
            {
                var userName = USERNAME_PREFIX + job.TenantName;

                // e.g. c:\applications\<tenantid>\<batchid>\<jobid>\
                string jobDirectory = Path.Combine(applicationsRootPath, userName, batchId, job.JobId);

                DeleteDirectory(jobDirectory);
            }
            catch { }
        }

        /// <summary>
        /// Download a blob to the local file system.
        /// </summary>
        /// <param name="container">Container path.</param>
        /// <param name="source">Blob path.</param>
        /// <param name="target">Local path.</param>
        /// <param name="credentials">Azure storage account credentials.</param>
        /// <remarks>
        /// Do not catch any exceptions here, let it bubble up the stack
        /// </remarks>
        private void DownloadBlob(string source, string target, string storageConn, string tenantId)
        {
            if (Directory.Exists(temporaryFilesRootPath) == false)
                Directory.CreateDirectory(temporaryFilesRootPath);

            CloudStorageAccount cs = CloudStorageAccount.Parse(storageConn);

            var client = cs.CreateCloudBlobClient();

            var container = client.GetContainerReference(CONTAINERNAME);

            var blobList = container.ListBlobs(tenantId, true).Select(b => b.Uri);

            var blobUri = blobList.Where(b => Path.GetFileName(b.LocalPath).Equals(source, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

            if (blobUri != null)
            {
                var blobBlock = container.GetBlockBlobReference(blobUri.ToString());

                using (var fileStream = System.IO.File.OpenWrite(target))
                {
                    blobBlock.DownloadToStream(fileStream);
                }
            }
            else
            {
                throw new Exception(
                    string.Format("Unable to find job package {0} in blob storage container 'jobprocessorfiles' under directory {1}. Make sure you have your package correctly deployed!",
                        source, tenantId)
                );
            }
        }

        /// <summary>
        /// Helper method to recursively delete all folders and files under and including the specified directory
        /// </summary>
        /// <param name="directory">The path to the phsical directory</param>
        private void DeleteDirectory(string directory)
        {
            if (Directory.Exists(directory) == false)
                return;

            string[] files = Directory.GetFiles(directory);

            string[] subDirectories = Directory.GetDirectories(directory);

            // set the normal attribute so that the file can be deleted
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                try
                {
                    File.Delete(file);
                }
                catch (UnauthorizedAccessException) { }
            }

            // remove all subdirectories under this directory - recursive method
            foreach (string subDirectory in subDirectories)
            {
                DeleteDirectory(subDirectory);
            }

            try
            {
                // remove this directory
                // the recursive is set to false because we are manually removing files and subdirectories as part of this routine
                Directory.Delete(directory, false);
            }
            catch (IOException) { }
        }

        /// <summary>
        /// Helper method that copies contents between directories for tenant-deployment
        /// </summary>
        private void CopyDirectory(string source, string destination)
        {
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(destination + dir.Substring(source.Length));
            }

            //Copy all the files
            foreach (string fileName in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                File.Copy(fileName, destination + fileName.Substring(source.Length));
            }
        }

        /// <summary>
        /// Tries creating a new, local windows user and returns the password on success
        /// </summary>
        private SecureString TryCreateNewUser(string userName)
        {
            UserPrincipal user;
            string userPassword = string.Empty;

            // Normalize the user name
            // FUTURE FEATURE: as soon as we run processes under different accounts, we need to make sure this is not longer than 20 characters.
            // FUTURE FEATURE: a Windows User Name cannot be longer than 20 characters. Therefore we need to fix the relationship between userName and tenant-name!!!
            // Note: currently the WebAPI enforces that tenant names are not longer than 15 characters, so we should be fine as soon as we create a Windows User
            userName = userName.ToLower();

            //
            // Passwords need to be cached since they need to be retrieved on subsequent calls of this manager
            // If a password is not found in the in-memory cache, it resets the password for this user.
            //
            lock (inMemoryPasswordCacheForThisHost)
            {
                using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                {
                    user = new UserPrincipal(context);
                    user.Name = userName;
                    var searcher = new PrincipalSearcher(user);
                    user = searcher.FindOne() as UserPrincipal;

                    if (user == null)
                    {
                        // create a password for the identity
                        userPassword = Membership.GeneratePassword(PASSWORD_LENGTH, PASSWORD_NONALPHANUMERICS);

                        try
                        {
                            user = new UserPrincipal(context);
                            user.Name = userName;
                            user.PasswordNeverExpires = true;
                            user.SetPassword(userPassword);
                            user.Save();
                        }
                        catch (PrincipalExistsException)
                        {
                            // Principal has been created, already, therefore no need to further create it.
                            // Should only happen on local debugging since we currently in the cloud execute one job / instance at one point in time.
                        }

                        // User created successfully, therefore add the the password to the in-memory cache
                        var protectedPassword = inMemoryPasswordProtector.Protect(System.Text.Encoding.UTF8.GetBytes(userPassword));
                        if (inMemoryPasswordCacheForThisHost.ContainsKey(userName))
                            inMemoryPasswordCacheForThisHost[userName] = protectedPassword;
                        else
                            inMemoryPasswordCacheForThisHost.Add(userName, protectedPassword);
                    }
                    else
                    {
                        // If the password exists in the cache, use the existing one
                        if (inMemoryPasswordCacheForThisHost.ContainsKey(userName))
                        {
                            var unprotectedPassword = inMemoryPasswordProtector.Unprotect(inMemoryPasswordCacheForThisHost[userName]);
                            userPassword = System.Text.Encoding.UTF8.GetString(unprotectedPassword);
                        }
                        else
                        {
                            // The cache is empty, the user exists. Maybe the in-Memory cache has been cleared due to an instance-recycle.
                            // Therefore in this case we set a new password for the user
                            userPassword = Membership.GeneratePassword(PASSWORD_LENGTH, PASSWORD_NONALPHANUMERICS);

                            user.SetPassword(userPassword);
                            user.Save();

                            // Finally save the password in the in-memory password cache for later executions
                            var protectedPassword = inMemoryPasswordProtector.Protect(System.Text.Encoding.UTF8.GetBytes(userPassword));
                            inMemoryPasswordCacheForThisHost.Add(userName, protectedPassword);
                        }
                    }

                    // Always try to add the user to the group to ensure it is part of it
                    GroupPrincipal group = new GroupPrincipal(context);
                    group.Name = USER_GROUP_NAME;
                    var groupSearcher = new PrincipalSearcher(group);

                    group = groupSearcher.FindOne() as GroupPrincipal;

                    if (group != null && group.Members.Contains(user) == false)
                    {
                        group.Members.Add(user);
                        group.Save();
                    }
                }
            }

            // Password or user generation went wrong
            if (string.IsNullOrEmpty(userPassword))
                return null;

            // Return the password for the user so the tenant create a complete context out of it
            var secureUserPassword = new SecureString();
            foreach (var c in userPassword)
                secureUserPassword.AppendChar(c);

            return secureUserPassword;
        }

        /// <summary>
        /// Delete a user from this machine.
        /// </summary>
        /// <param name="userName">Username of user to remove.</param>
        private void DeleteUsers()
        {
            lock (inMemoryPasswordCacheForThisHost)
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                    {
                        foreach (var user in searcher.FindAll().Where(u => u.Name.StartsWith(USERNAME_PREFIX)))
                        {
                            user.Delete();
                            if (inMemoryPasswordCacheForThisHost.ContainsKey(user.Name))
                                inMemoryPasswordCacheForThisHost.Remove(user.Name);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Grants permissions to the specified folder for the
        /// specified user.
        /// </summary>
        /// <param name="path">Full path to the folder.</param>
        /// <param name="username">Username to grant access for.</param>
        private void AddUserPermissionsToFolder(string path, string username, FileAccessPermission permission)
        {
            #region Old way of setting permissions with ICACLS as external process

            //switch (permission)
            //{
            //    case FileAccessPermission.ReadExecute:
            //        {
            //            System.Diagnostics.Process.Start("ICACLS", path + " /grant:r " + username + ":(OI)(CI)RX /T");
            //        }
            //        break;
            //    case FileAccessPermission.Full:
            //        {
            //            System.Diagnostics.Process.Start("ICACLS", path + " /grant:r " + username + ":(OI)(CI)F /T");
            //        }
            //        break;
            //}

            #endregion 

            var dirInfo = new DirectoryInfo(path);
            var dirSecurity = dirInfo.GetAccessControl();

            switch (permission)
            {
                case FileAccessPermission.ReadExecute:
                    dirSecurity.AddAccessRule(
                            new FileSystemAccessRule
                                    (
                                        username, 
                                        FileSystemRights.ReadAndExecute | FileSystemRights.ExecuteFile | FileSystemRights.ListDirectory,
                                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                        PropagationFlags.None,
                                        AccessControlType.Allow
                                    )
                        );
                    break;

                case FileAccessPermission.Full:
                    dirSecurity.AddAccessRule(
                        new FileSystemAccessRule
                                (
                                    username,
                                    FileSystemRights.FullControl,
                                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                    PropagationFlags.InheritOnly,
                                    AccessControlType.Allow
                                )
                    );
                    break;

            }

            dirInfo.SetAccessControl(dirSecurity);
        }
    }

    /// <summary>
    /// Enumeration to represent possible file permiossions
    /// used by the provisioning engine when creating folders.
    /// </summary>
    public enum FileAccessPermission
    {
        ReadExecute = 0,
        Full = 1
    }
}
