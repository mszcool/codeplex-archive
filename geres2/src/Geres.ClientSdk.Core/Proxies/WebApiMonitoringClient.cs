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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Geres.ClientSdk.Core.Proxies
{
    internal class WebApiMonitoringClient : IMonitoringServiceClient
    {
        private readonly string _apiBaseUrl = "/api/jobmonitor";
        private readonly string _getBatchesUrl = "/batch";
        private readonly string _getJobDeteailsUrl = "/job/{0}";
        private readonly string _getBatchDetailsUrl = "/batch/{0}";

        private string _baseUrl;
        private HttpClient _httpClient;
        private JsonSerializer _jsonSerializer;

        internal WebApiMonitoringClient(string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(string.Concat(baseUrl, _apiBaseUrl));
            _httpClient.DefaultRequestHeaders.Add
                (
                    HttpRequestHeader.ContentType.ToString(),
                    "application/json"
                );

            _jsonSerializer = Newtonsoft.Json.JsonSerializer.Create();

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

        #region IMonitoringServiceClient implementation

        public string CurrentPagingToken { get; set; }

        public int PageSize { get; set; }

        public async Task<List<Batch>> GetBatches()
        {
            string webApiCallUrl = string.Concat(_baseUrl, _getBatchesUrl);
            var resp = await _httpClient.GetAsync(webApiCallUrl);
            resp.EnsureSuccessStatusCode();

            var batchJson = await resp.Content.ReadAsStringAsync();
            using (StringReader sr = new StringReader(batchJson))
            {
                var batches = _jsonSerializer.Deserialize<List<Batch>>
                                    (
                                        new JsonTextReader(sr)
                                    );
                return batches;
            }
        }

        public async Task<Common.Entities.Job> GetJobDetails(string jobId, string batchId = "")
        {
            string webApiCallUrl = string.Format(string.Concat(_baseUrl, _getJobDeteailsUrl), jobId);
            if (!string.IsNullOrEmpty(batchId))
                webApiCallUrl = string.Concat(webApiCallUrl, "?batchId=", batchId);

            var resp = await _httpClient.GetAsync(webApiCallUrl);
            resp.EnsureSuccessStatusCode();
            
            var jobJson = await resp.Content.ReadAsStringAsync();
            using (StringReader sr = new StringReader(jobJson))
            {
                var job = _jsonSerializer.Deserialize<Job>
                                (
                                    new JsonTextReader(sr)
                                );
                return job;
            }
        }

        public async Task<List<Common.Entities.Job>> GetJobsFromBatch(string batchId)
        {
            var result = await GetJobsFromBatch(batchId, string.Empty);
            return result;
        }

        public async Task<List<Common.Entities.Job>> GetJobsFromBatch(string batchId, string filterQueryString)
        {
            var webApiCallUrl = string.Format(string.Concat(_baseUrl, _getBatchDetailsUrl), batchId);
            if(PageSize <= 0 || PageSize > 1000)
                PageSize = 1000;

            webApiCallUrl = string.Concat(webApiCallUrl, "?numOfRecords=", PageSize.ToString());
            if (!string.IsNullOrEmpty(CurrentPagingToken))
                webApiCallUrl = string.Concat(webApiCallUrl, "&continuationToken=", CurrentPagingToken);
            if (!string.IsNullOrEmpty(filterQueryString))
                webApiCallUrl = string.Concat(webApiCallUrl, "&$filter=", filterQueryString);

            var batchResp = await _httpClient.GetAsync(webApiCallUrl);
            batchResp.EnsureSuccessStatusCode();

            if (batchResp.Headers.Contains("gerespagingtoken"))
                CurrentPagingToken = batchResp.Headers.GetValues("gerespagingtoken").FirstOrDefault();
            else
                CurrentPagingToken = null;

            var batchRespJson = await batchResp.Content.ReadAsStringAsync();
            using (StringReader sr = new StringReader(batchRespJson))
            {
                var jobs = _jsonSerializer.Deserialize<List<Job>>
                                (
                                    new JsonTextReader(sr)
                                );
                return jobs;
            }
        }

        #endregion
    }
}
