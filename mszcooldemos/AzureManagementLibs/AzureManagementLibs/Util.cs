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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AzureManagementLibs
{
    internal static class Util
    {
        internal static SubscriptionCloudCredentials GetCredentials(string subscriptionId, string certificateCN)
        {
            X509Certificate2 cert = null;

            //
            // Get the certificate from the store
            //
            var certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                certStore.Open(OpenFlags.ReadOnly);
                var certs = certStore.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, certificateCN, false);

                if (certs.Count == 0)
                    throw new Exception("Unable to find management certificate in store!");
                else
                    cert = certs[0];
            }
            finally
            {
                certStore.Close();
            }

            // 
            // Now create the credentials for the management library
            //
            return new CertificateCloudCredentials(subscriptionId, cert);
        }

        internal static void EvaluateResult(OperationResponse operationResponse)
        {
            var statusCode = (int)operationResponse.StatusCode;
            if (statusCode < 200 || statusCode > 299)
                throw new Exception("Operation did not result in an HTTP-success code. Returned code: " + operationResponse.StatusCode.ToString());
        }
    }
}
