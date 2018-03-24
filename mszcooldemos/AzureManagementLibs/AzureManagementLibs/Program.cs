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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Storage.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureManagementLibs
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.CancellationToken cancellationToken = new System.Threading.CancellationToken();

            try
            {
                #region Read & Create Cloud Credentials

                Console.WriteLine("Reading settings from configuration...");
                var subscriptionId = ConfigurationManager.AppSettings["subscriptionId"];
                var certificateCN = ConfigurationManager.AppSettings["certificateSubjectCN"];
                Console.WriteLine("- Using subscription: {0}", subscriptionId);
                Console.WriteLine("- Using certificate: {0}", certificateCN);

                Console.WriteLine();
                Console.WriteLine("Creating cloud credentials...");
                var cloudMgmtCredentials = Util.GetCredentials(subscriptionId, certificateCN);

                #endregion

                #region Create Storage Management Clients

                Console.WriteLine();
                Console.WriteLine("Creating virtual machine and virtual network management client...");
                var vmManagementClient = CloudContext.Clients.CreateComputeManagementClient(cloudMgmtCredentials);
                var vNetManagementClient = CloudContext.Clients.CreateVirtualNetworkManagementClient(cloudMgmtCredentials);
                var storageManagementClient = CloudContext.Clients.CreateStorageManagementClient(cloudMgmtCredentials);

                #endregion

                #region List Virtual Machine Images

                Console.WriteLine();
                Console.WriteLine("Reading list of virtual machine images of the user and for the Windows OS...");
                var vmImages = vmManagementClient.VirtualMachineImages.ListAsync(cancellationToken).Result;
                var vmImagesToShow = (from vi in vmImages
                                      where (vi.Category == "Public" && vi.PublisherName == "Microsoft Windows Server Group") ||
                                            (vi.Category == "User")
                                      select vi).ToList();
                for (int i = 0; i < vmImagesToShow.Count; i++)
                {
                    var vi = vmImagesToShow[i];
                    Console.WriteLine("{0}\t{1}\t{2}", i, vi.Label, vi.PublishedDate);
                }

                #endregion

                #region User Input

                Console.WriteLine();
                Console.Write("--> Enter your hosted service name: ");
                var hostedServiceName = Console.ReadLine();
                Console.Write("--> Enter a name for your deployment: ");
                var deploymentName = Console.ReadLine();
                Console.Write("--> Also create VMs (y/n)? ");
                var alsoCreate = Console.ReadLine().ToLower();

                int imageIdx = 0, numImages = 0;
                string location = "", adminUser = "", adminPwd = "";
                if (alsoCreate.Equals("y"))
                {
                    Console.Write("--> Select number of the image to create: ");
                    imageIdx = Int32.Parse(Console.ReadLine());
                    Console.Write("--> Select number of images to create: ");
                    numImages = Int32.Parse(Console.ReadLine());
                    Console.Write("--> Enter your location: ");
                    location = Console.ReadLine();
                    Console.Write("--> Enter your Admin user name: ");
                    adminUser = Console.ReadLine();
                    Console.Write("--> Enter your Admin password: ");
                    adminPwd = Console.ReadLine();
                }

                #endregion

                if (alsoCreate.Equals("y"))
                {
                    #region Create Hosted Service (Cloud Service)

                    Console.WriteLine();
                    Console.WriteLine("Creating the hosted service...");
                    var hostedServiceTask = vmManagementClient.HostedServices.CreateAsync
                                                (
                                                    new HostedServiceCreateParameters()
                                                    {
                                                        ServiceName = hostedServiceName,
                                                        Label = hostedServiceName,
                                                        Location = location
                                                    },
                                                    cancellationToken
                                                );
                    while (!hostedServiceTask.IsCompleted)
                    {
                        Console.Write(".");
                        System.Threading.Thread.Sleep(1000);
                    }
                    Util.EvaluateResult(hostedServiceTask.Result);

                    #endregion

                    #region Create Storage Account

                    Console.WriteLine();
                    Console.WriteLine("Creating the storage account...");
                    var storageAccountTask = storageManagementClient.StorageAccounts.CreateAsync
                                                (
                                                    new StorageAccountCreateParameters()
                                                    {
                                                        Description = string.Format("Storage for {0} VMs", hostedServiceName),
                                                        GeoReplicationEnabled = false,
                                                        Label = string.Format("Storage for {0} VMs", hostedServiceName),
                                                        ServiceName = hostedServiceName,
                                                        Location = location
                                                    },
                                                    cancellationToken
                                                );
                    while (!storageAccountTask.IsCompleted)
                    {
                        Console.Write(".");
                        System.Threading.Thread.Sleep(1000);
                    }
                    Util.EvaluateResult(storageAccountTask.Result);

                    #endregion

                    #region Create deployment & first VM

                    Console.WriteLine();
                    Console.WriteLine("Creating deployment with first VM...");
                    var deploymentTask = vmManagementClient.VirtualMachines.CreateDeploymentAsync
                                            (
                                                hostedServiceName,
                                                new VirtualMachineCreateDeploymentParameters()
                                                {
                                                    DeploymentSlot = DeploymentSlot.Production,
                                                    Label = deploymentName,
                                                    Name = deploymentName,
                                                    Roles = new List<Role>
                                                {
                                                    new Role() 
                                                    {
                                                        RoleName = "myvm0",
                                                        RoleSize = "Small",
                                                        RoleType = "PersistentVMRole",
                                                        OSVirtualHardDisk = new OSVirtualHardDisk()
                                                        {
                                                            DiskLabel = "VM 0 OS Disk 0",
                                                            DiskName = string.Format("vm{0}{1}os", hostedServiceName, "0"),
                                                            SourceImageName = vmImagesToShow[imageIdx].Name,
                                                            MediaLink = new Uri(string.Format("http://{0}.blob.core.windows.net/vhds/vm{1}{2}os.vhd", hostedServiceName, hostedServiceName, "0"))
                                                        },
                                                        ConfigurationSets = new List<ConfigurationSet> 
                                                        {
                                                            new ConfigurationSet() 
                                                            {
                                                                AdminUserName = adminUser,
                                                                AdminPassword = adminPwd,
                                                                HostName = "vm0",
                                                                ComputerName = "vm0",
                                                                ConfigurationSetType = "WindowsProvisioningConfiguration"
                                                            }
                                                        }
                                                    }
                                                }
                                                },
                                                cancellationToken
                                            );
                    while (!deploymentTask.IsCompleted)
                    {
                        Console.Write(".");
                        System.Threading.Thread.Sleep(1000);
                    }
                    Util.EvaluateResult(deploymentTask.Result);

                    #endregion

                    #region Create remaining VMs

                    Console.WriteLine();
                    Console.WriteLine("Creating further virtual machines...");
                    var imageToUse = vmImagesToShow[imageIdx];
                    for (int i = 1; i < numImages; i++)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Creating VM {0}", i);

                        var configurationSet = new ConfigurationSet()
                        {
                            AdminUserName = adminUser,
                            AdminPassword = adminPwd,
                            HostName = string.Format("vm{0}", i),
                            ComputerName = string.Format("vm{0}", i),
                            ConfigurationSetType = "WindowsProvisioningConfiguration"
                        };
                        var configurationSets = new List<ConfigurationSet>();
                        configurationSets.Add(configurationSet);

                        var vmCreationTask = vmManagementClient.VirtualMachines.CreateAsync
                                                (
                                                    hostedServiceName,
                                                    deploymentName,
                                                    new VirtualMachineCreateParameters()
                                                    {
                                                        RoleName = string.Format("myvm{0}", i),
                                                        RoleSize = "Small",
                                                        OSVirtualHardDisk = new OSVirtualHardDisk()
                                                        {
                                                            DiskLabel = string.Format("VM {0} OS Disk", i),
                                                            DiskName = string.Format("vm{0}{1}os", hostedServiceName, i),
                                                            SourceImageName = vmImagesToShow[imageIdx].Name,
                                                            MediaLink = new Uri(string.Format("http://{0}.blob.core.windows.net/vhds/vm{0}{1}os", hostedServiceName, i))
                                                        },
                                                        ConfigurationSets = configurationSets
                                                    },
                                                    cancellationToken
                                                );

                        while (!vmCreationTask.IsCompleted)
                        {
                            Console.Write(".");
                            System.Threading.Thread.Sleep(1000);
                        }

                        try
                        {
                            Util.EvaluateResult(deploymentTask.Result);
                            Console.WriteLine("\r\nCreation of VM {0} succeeded!", i);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Creation of VM {0} failed: {1} {2}", i,
                                                ex.Message,
                                                (ex.InnerException != null ? ex.InnerException.Message : ""));
                        }
                    }

                    #endregion
                }

                #region List VMs in Cloud Service

                Console.WriteLine();
                Console.WriteLine("Listing virtual machines in hosted service {0}...", hostedServiceName);

                Console.WriteLine("Getting deployment details for hosted service...");
                var deploymentGetTask = vmManagementClient.Deployments.GetByNameAsync(hostedServiceName, deploymentName, cancellationToken);
                while (!deploymentGetTask.IsCompleted)
                {
                    Console.Write(".");
                    System.Threading.Thread.Sleep(1000);
                }
                var deploymentGetResponse = (DeploymentGetResponse)deploymentGetTask.Result;
                Console.WriteLine("Deployment details retrieved:");
                Console.WriteLine("- Deployment name: {0}", deploymentGetResponse.Name);
                Console.WriteLine("- Deployment slot: {0}", deploymentGetResponse.DeploymentSlot);
                Console.WriteLine("- Deployment Role Instances:");
                Console.WriteLine("    {0,20}{1,20}{2,20}", "HostName", "InstanceName", "Status");
                foreach (var vmInstance in deploymentGetResponse.RoleInstances)
                {
                    Console.WriteLine("  {0,20}{1,20}{2,20}", vmInstance.HostName, vmInstance.InstanceName, vmInstance.InstanceStatus);
                }

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured: {0}{1}{2}!",
                                    ex.Message,
                                    Environment.NewLine,
                                    ex.StackTrace);
            }

            Console.WriteLine();
            Console.WriteLine("Press ENTER to quit...");
            Console.ReadLine();
        }
    }
}
