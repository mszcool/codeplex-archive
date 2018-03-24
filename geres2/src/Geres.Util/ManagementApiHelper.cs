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
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Geres.Util
{
    public static class ManagementApiHelper
    {
        private static string versionNumber = "2013-08-01";
        private static XNamespace wa = "http://schemas.microsoft.com/windowsazure";
        private static XNamespace sc = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";
        private static string getHostedServicesOperationUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices";
        private static string getHostedServicePropertyOperationUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices/{1}?embed-detail=true";
        private static string getHostedServiceConfigurationUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/production";
        private static string updateConfigurationUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/production/?comp=config";
        private static string deleteRoleInstancesUrlTemplate = "https://management.core.windows.net/{0}/services/hostedservices/{1}/deploymentslots/production/roleinstances/?comp=delete";

        public static string AddJobHostInstances(string processRoleName, int newCount)
        {
            if (string.IsNullOrWhiteSpace(GlobalConstants.PUBLISHSETTINGS_FILE_NAME))
                throw new ArgumentNullException("publishSettingsFilePath");

            if (!RoleEnvironment.IsAvailable)
                throw new ApplicationException("Not running in Azure");

            var cert = GetCert(GlobalConstants.PUBLISHSETTINGS_FILE_NAME);
            if (null == cert)
                throw new ApplicationException("No certificate");

            var subscriptionId = GetSubscriptionId(GlobalConstants.PUBLISHSETTINGS_FILE_NAME);
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ApplicationException("No SubscriptionId");

            var hostedService = GetHostedServiceName(subscriptionId, cert);
            if (string.IsNullOrWhiteSpace(hostedService))
                throw new ApplicationException("Could not find role details");

            var configuration = GetConfiguration(subscriptionId, cert, hostedService);

            var requestId = string.Empty;

            try
            {
                SetInstanceCount(processRoleName, configuration, newCount);

                requestId = UploadConfiguration(subscriptionId, hostedService, cert, configuration);
            }
            catch { }

            return requestId;
        }

        public static string RemoveJobHostInstances(string[] roleInstances)
        {
            if (string.IsNullOrWhiteSpace(GlobalConstants.PUBLISHSETTINGS_FILE_NAME))
                throw new ArgumentNullException("publishSettingsFilePath");

            if (!RoleEnvironment.IsAvailable)
                throw new ApplicationException("Not running in Azure");

            var cert = GetCert(GlobalConstants.PUBLISHSETTINGS_FILE_NAME);
            if (null == cert)
                throw new ApplicationException("No certificate");

            var subscriptionId = GetSubscriptionId(GlobalConstants.PUBLISHSETTINGS_FILE_NAME);
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ApplicationException("No SubscriptionId");

            var hostedService = GetHostedServiceName(subscriptionId, cert);
            if (string.IsNullOrWhiteSpace(hostedService))
                throw new ApplicationException("Could not find role details");

            var configuration = GetConfiguration(subscriptionId, cert, hostedService);

            var requestId = string.Empty;

            try
            {
                requestId = DeleteRoleInstances(subscriptionId, hostedService, cert, roleInstances);
            }
            catch { }

            return requestId;
        }

        public static string CheckProgress(string requestId)
        {
            if (string.IsNullOrWhiteSpace(GlobalConstants.PUBLISHSETTINGS_FILE_NAME))
                throw new ArgumentNullException("publishSettingsFilePath");

            if (!RoleEnvironment.IsAvailable)
                throw new ApplicationException("Not running in Azure");

            var cert = GetCert(GlobalConstants.PUBLISHSETTINGS_FILE_NAME);
            if (null == cert)
                throw new ApplicationException("No certificate");

            var subscriptionId = GetSubscriptionId(GlobalConstants.PUBLISHSETTINGS_FILE_NAME);
            if (string.IsNullOrWhiteSpace(subscriptionId))
                throw new ApplicationException("No SubscriptionId");


            string status = string.Empty;

            if (!string.IsNullOrEmpty(requestId))
            {
                status = GetStatus(requestId, subscriptionId, cert);
            }

            return status;
        }

        private static string GetStatus(string requestId, string subscriptionId, X509Certificate2 certificate)
        {
            string uriFormat = "https://management.core.windows.net/{0}/operations/{1}";
            Uri uri = new Uri(String.Format(uriFormat, subscriptionId, requestId));
            var request = CreateHttpWebRequest(uri, certificate, "GET");
            XDocument responseBody = null;
            HttpStatusCode statusCode;
            var status = string.Empty;
            HttpWebResponse response;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                // GetResponse throws a WebException for 400 and 500 status codes
                response = (HttpWebResponse)ex.Response;
            }

            statusCode = response.StatusCode;

            if (response.ContentLength > 0)
            {
                using (XmlReader reader = XmlReader.Create(response.GetResponseStream()))
                {
                    responseBody = XDocument.Load(reader);

                    var operation = responseBody.Element(wa + "Operation");

                    status = operation.Element(wa + "Status").Value;
                }
            }

            response.Close();

            return status;
        }

        private static string GetHostedServiceName(string subscriptionId, X509Certificate2 certificate)
        {
            var hostedServices = GetHostedServices(subscriptionId, certificate);
            var deploymentId = RoleEnvironment.DeploymentId;

            foreach (var hostedService in hostedServices)
            {
                var xe = GetHostedServiceProperties(subscriptionId, certificate, hostedService);
                if (xe == null) continue;

                var deploymentXElements = xe.Elements(XName.Get("Deployments", "http://schemas.microsoft.com/windowsazure")).Elements(XName.Get("Deployment", "http://schemas.microsoft.com/windowsazure")).ToList();
                if (deploymentXElements == null || deploymentXElements.Count == 0) continue;

                foreach (var deployment in deploymentXElements)
                {
                    var currentDeploymentId = deployment.Element(XName.Get("PrivateID", "http://schemas.microsoft.com/windowsazure")).Value;
                    if (currentDeploymentId == deploymentId) return hostedService;
                }
            }

            return null;
        }

        private static X509Certificate2 GetCert(string filename)
        {
            var managementCertbase64string = XDocument.Load(filename).Descendants("PublishProfile").First().Attribute("ManagementCertificate").Value;
            return new X509Certificate2(Convert.FromBase64String(managementCertbase64string));
        }

        private static string GetSubscriptionId(string filename)
        {
            return XDocument.Load(filename).Descendants("Subscription").First().Attribute("Id").Value;
        }

        private static IEnumerable<string> GetHostedServices(string subscriptionId, X509Certificate2 certificate)
        {
            string uri = string.Format(getHostedServicesOperationUrlTemplate, subscriptionId);
            var requestUri = new Uri(uri);
            var httpWebRequest = CreateHttpWebRequest(requestUri, certificate, "GET");
            using (var response = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                XElement xe = XElement.Load(responseStream);

                if (xe == null) return new string[] { };
                var serviceNameElements = xe.Elements().Elements(XName.Get("ServiceName", wa.ToString()));
                return serviceNameElements.Select(x => x.Value);
            }
        }

        private static XElement GetHostedServiceProperties(string subscriptionId, X509Certificate2 certificate, string hostedServiceName)
        {
            var uri = string.Format(getHostedServicePropertyOperationUrlTemplate, subscriptionId, hostedServiceName);
            var requestUri = new Uri(uri);
            var httpWebRequest = CreateHttpWebRequest(requestUri, certificate, "GET");
            using (var response = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                return XElement.Load(responseStream);
            }
        }

        private static HttpWebRequest CreateHttpWebRequest(Uri uri, X509Certificate2 certificate, string httpWebRequestMethod)
        {
            var httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(uri);
            httpWebRequest.Method = httpWebRequestMethod;
            httpWebRequest.Headers.Add("x-ms-version", versionNumber);
            httpWebRequest.ClientCertificates.Add(certificate);
            httpWebRequest.ContentType = "application/xml";
            return httpWebRequest;
        }

        private static string UploadConfiguration(string subscriptionId, string hostedServiceName, X509Certificate2 certificate, XElement configuration)
        {
            String configurationString = configuration.ToString();
            String base64Configuration = ConvertToBase64String(configurationString);
            XElement xConfiguration = new XElement(wa + "Configuration", base64Configuration);
            XElement xChangeConfiguration = new XElement(wa + "ChangeConfiguration", xConfiguration);
            XDocument payload = new XDocument();
            payload.Add(xChangeConfiguration);
            payload.Declaration = new XDeclaration("1.0", "UTF-8", "no");

            string uriFormat = updateConfigurationUrlTemplate;

            Uri uri = new Uri(String.Format(uriFormat, subscriptionId, hostedServiceName));

            HttpWebRequest httpWebRequest = CreateHttpWebRequest(uri, certificate, "POST");

            using (Stream requestStream = httpWebRequest.GetRequestStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(requestStream, System.Text.UTF8Encoding.UTF8))
                {
                    payload.Save(streamWriter, SaveOptions.DisableFormatting);
                }
            }

            String requestId = string.Empty;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    requestId = response.Headers["x-ms-request-id"];
                }
            }
            catch (Exception)
            { }

            return requestId;
        }

        public static XElement GetConfiguration(string subscriptionId, X509Certificate2 certificate, string hostedServiceName)
        {
            var uri = string.Format(getHostedServiceConfigurationUrlTemplate, subscriptionId, hostedServiceName);
            var requestUri = new Uri(uri);
            var httpWebRequest = CreateHttpWebRequest(requestUri, certificate, "GET");

            using (var response = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                using (XmlReader reader = XmlReader.Create(response.GetResponseStream()))
                {
                    var responseBody = XDocument.Load(reader);
                    String base64Configuration = responseBody.Element(wa + "Deployment").Element(wa + "Configuration").Value;
                    String stringConfiguration = ConvertFromBase64String(base64Configuration);
                    return XElement.Parse(stringConfiguration);
                }
            }
        }

        public static Int32 GetInstanceCount(string processRoleName, XElement configuration)
        {
            XElement instanceElement = (from s in configuration.Elements(sc + "Role")
                                        where s.Attribute("name").Value == processRoleName
                                        select s.Element(sc + "Instances")).First();

            Int32 instanceCount = (Int32)Convert.ToInt32(instanceElement.Attribute("count").Value);

            return instanceCount;
        }

        private static void SetInstanceCount(string processRoleName, XElement configuration, Int32 value)
        {

            XElement instanceElement = (from s in configuration.Elements(sc + "Role")
                                        where s.Attribute("name").Value == processRoleName
                                        select s.Element(sc + "Instances")).First();
            instanceElement.SetAttributeValue("count", value);
        }

        private static String ConvertFromBase64String(String base64Value)
        {
            Byte[] bytes = Convert.FromBase64String(base64Value);
            String value = System.Text.Encoding.UTF8.GetString(bytes);
            return value;
        }

        private static String ConvertToBase64String(String value)
        {
            Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            String base64String = Convert.ToBase64String(bytes); return base64String;
        }

        private static string DeleteRoleInstances(string subscriptionId, string cloudServiceName, X509Certificate2 cert, string[] roleInstanceNames)
        {
            var uri = string.Format(deleteRoleInstancesUrlTemplate, subscriptionId, cloudServiceName);

            var requestBodyFormat = @"<RoleInstances xmlns=""http://schemas.microsoft.com/windowsazure"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">{0}</RoleInstances>";
            var namesXml = string.Join("", roleInstanceNames.Select(x => string.Format("<Name>{0}</Name>", x)));

            var requestUri = new Uri(uri);
            var httpWebRequest = CreateHttpWebRequest(requestUri, cert, "POST");

            var requestBody = Encoding.UTF8.GetBytes(string.Format(requestBodyFormat, namesXml));
            using (var stream = httpWebRequest.GetRequestStream())
            {
                stream.Write(requestBody, 0, requestBody.Length);
            }

            String requestId;

            using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                requestId = response.Headers["x-ms-request-id"];
            }

            return requestId;
        }
    }
}
