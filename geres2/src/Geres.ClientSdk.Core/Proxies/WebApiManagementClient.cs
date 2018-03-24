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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace Geres.ClientSdk.Core.Proxies
{
    internal class WebApiManagementClient : IManagementServiceClient
    {
        private HttpClient _httpClient;
        private string _baseUrl;
        private readonly string _apiBaseUrl = "/api/jobcontrol";
        private readonly string _submitJobUrl = "/jobs/{0}";
        private readonly string _submitJobsUrl = "/jobs/list/{0}";
        private readonly string _cancelJobUrl = "/jobs/{0}";
        private readonly string _createBatchUrl = "/batch";
        private readonly string _closeBatchUrl = "/batch/close/{0}";
        private readonly string _cancelBatchUrl = "/batch/delete/{0}";

        internal WebApiManagementClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = 
                new Uri(string.Concat(baseUrl, _apiBaseUrl));
            _httpClient.DefaultRequestHeaders.Add
                (
                    HttpRequestHeader.ContentType.ToString(),
                    "application/json"
                );

            _baseUrl = _httpClient.BaseAddress.ToString();
        }

        internal void SetAuthenticationToken(string authenticationToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue
                    (
                        "Bearer",
                        authenticationToken
                    );
        }

        #region IManagementServiceClient implementation

        public async Task<string> SubmitJob(Common.Entities.Job newJob, string batchId = "")
        {
            var webApiCallUrl = string.Format(string.Concat(_baseUrl, _submitJobUrl), batchId);
            var jobPostResp = await _httpClient.PostAsync<Job>
                                    (
                                        webApiCallUrl,
                                        newJob,
                                        new JsonMediaTypeFormatter()
                                    );
            jobPostResp.EnsureSuccessStatusCode();
            var jobId = await jobPostResp.Content.ReadAsAsync<string>();
            return jobId;
        }

        public async Task<List<string>> SubmitJobs(List<Common.Entities.Job> newJobs, string batchId = "")
        {
            var webApiCallUrl = string.Format(string.Concat(_baseUrl, _submitJobsUrl), batchId);
            var jobPostResp = await _httpClient.PostAsync<List<Job>>
                                    (
                                        webApiCallUrl,
                                        newJobs,
                                        new JsonMediaTypeFormatter()
                                    );
            jobPostResp.EnsureSuccessStatusCode();
            var jobIds = await jobPostResp.Content.ReadAsAsync<List<string>>();
            return jobIds;
        }

        public async Task CancelJob(string jobId)
        {
            var webApiCallUrl = string.Format(string.Concat(_baseUrl, _cancelJobUrl), jobId);
            var jobPostResp = await _httpClient.DeleteAsync(webApiCallUrl);
            jobPostResp.EnsureSuccessStatusCode();
        }

        public async Task<string> CreateBatch(Common.Entities.Batch newBatch)
        {
            string webApiUrlForCall = string.Concat(_baseUrl, _createBatchUrl);
            var responseBatchCreate = await _httpClient.PutAsync<Batch>
                                                (
                                                    webApiUrlForCall, 
                                                    newBatch, 
                                                    new JsonMediaTypeFormatter()
                                                );
            responseBatchCreate.EnsureSuccessStatusCode();
            var batchId = await responseBatchCreate.Content.ReadAsAsync<string>();
            return batchId;
        }

        public async Task CloseBatch(string batchId)
        {
            var webApiCallUrl = string.Format(string.Concat(_baseUrl, _closeBatchUrl), batchId);
            var batchResp = await _httpClient.DeleteAsync(webApiCallUrl);
            batchResp.EnsureSuccessStatusCode();
        }

        public async Task CancelBatch(string batchId)
        {
            var webApiCallUrl = string.Format(string.Concat(_baseUrl, _cancelBatchUrl), batchId);
            var batchResp = await _httpClient.DeleteAsync(webApiCallUrl);
            batchResp.EnsureSuccessStatusCode();
        }

        #endregion
    }
}
