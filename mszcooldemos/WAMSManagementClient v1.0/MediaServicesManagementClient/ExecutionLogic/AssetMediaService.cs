using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaServicesManagementClient.ExecutionLogic
{
    public interface IAssetMediaService
    {
        IList<string> GetEncodingPresets();

        Task<IList<IAsset>> GetExistingAssets();

        void InitMediaContext();
        void InitMediaContext(string mediaServiceName, string mediaAccountKey);

        Task<string> IngestFile(string assetName, string fileName, string mimeType);
        event EventHandler<Dictionary<string, string>> IngestFileUploadProgressChanged;

        Task<IJob> EncodeAsset(string assetId, string targetFormat);
        event EventHandler<Dictionary<string, string>> EncodingJobProgressChanged;

        Task<IList<IJob>> GetAllJobs();

        Task<string> PublishAsset(string assetId);
    }

    public class AssetMediaService : IAssetMediaService
    {
        private string _mediaServiceName, _mediaServiceKey;

        private CloudMediaContext _cloudMediaContext = null;

        public AssetMediaService(string mediaServiceName, string mediaServiceKey)
        {
            _mediaServiceName = mediaServiceName;
            _mediaServiceKey = mediaServiceKey;
        }

        public void InitMediaContext()
        {
            InitMediaContext(_mediaServiceName, _mediaServiceKey);
        }

        public void InitMediaContext(string mediaServiceName, string mediaAccountKey)
        {
            _cloudMediaContext = new CloudMediaContext(mediaServiceName, mediaAccountKey);
        }

        public IList<string> GetEncodingPresets()
        {
            List<string> presetsSupportedByThisApp = new List<string>();
            presetsSupportedByThisApp.Add("VC1 Broadband 720p");
            presetsSupportedByThisApp.Add("VC1 Smooth Streaming 720p");
            presetsSupportedByThisApp.Add("H264 Broadband 720p");
            presetsSupportedByThisApp.Add("H264 Broadband 1080p");
            return presetsSupportedByThisApp;
        }

        #region Retrieving data from Media Services
        
        public Task<IList<IAsset>> GetExistingAssets()
        {
            Task<IList<IAsset>> t1 = new Task<IList<IAsset>>(() =>
            {
                var results = (from a in _cloudMediaContext.Assets
                               select a).ToList();
                return results;
            });

            t1.Start();

            return t1;
        }

        public Task<IList<IJob>> GetAllJobs()
        {
            Task<IList<IJob>> t1 = new Task<IList<IJob>>(() =>
            {
                return _cloudMediaContext.Jobs.ToList();
            });

            t1.Start();

            return t1;
        }

        #endregion

        #region Ingest Logic

        public event EventHandler<Dictionary<string, string>> IngestFileUploadProgressChanged;

        public Task<string> IngestFile(string assetName, string fileName, string mimeType)
        {
            var t = Task.Run(async () =>
            {
                // Create the asset
                IAsset inputAsset = _cloudMediaContext.Assets.Create(assetName, AssetCreationOptions.None);

                // The ingest the content
                BlobTransferClient blobTransferClient = new BlobTransferClient();
                blobTransferClient.NumberOfConcurrentTransfers = 2;
                blobTransferClient.ParallelTransferThreadCount = 2;

                IAccessPolicy accessPolicy = _cloudMediaContext.AccessPolicies.Create(fileName, TimeSpan.FromHours(4), AccessPermissions.List | AccessPermissions.Write);
                ILocator locator = _cloudMediaContext.Locators.CreateLocator(LocatorType.Sas, inputAsset, accessPolicy);

                IAssetFile assetFile = inputAsset.AssetFiles.Create(System.IO.Path.GetFileName(fileName));
                assetFile.UploadProgressChanged += assetFile_UploadProgressChanged;

                await assetFile.UploadAsync(fileName, blobTransferClient, locator, CancellationToken.None);
                assetFile.MimeType = mimeType;
                assetFile.Update();

                // Finally return the asset ID
                return inputAsset.Id;
            });

            return t;
        }

        private void assetFile_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            if (IngestFileUploadProgressChanged != null)
            {
                Dictionary<string, string> values = new Dictionary<string, string>();
                values.Add("Progress", e.Progress.ToString());
                values.Add("TotalBytes", e.TotalBytes.ToString());
                values.Add("BytesUploaded", e.BytesUploaded.ToString());

                IngestFileUploadProgressChanged(this, values);
            }
        }

        #endregion

        #region Encoding Logic

        public event EventHandler<Dictionary<string, string>> EncodingJobProgressChanged;

        public Task<IJob> EncodeAsset(string assetId, string targetFormat)
        {
            var t = Task<IJob>.Run(async () =>
                {
                    IMediaProcessor processor = _cloudMediaContext.MediaProcessors
                                                                  .Where(p => p.Name == "Windows Azure Media Encoder")
                                                                  .ToList().OrderBy(p => new Version(p.Version)).Last();

                    // Find the asset by ID
                    var asset = _cloudMediaContext.Assets.Where(a => a.Id == assetId).First();

                    // Create the job with a task to convert the asset
                    var job = _cloudMediaContext.Jobs.Create
                        (
                            string.Format("Job on {0} at {1} to encode {2}", 
                                DateTime.Now.ToShortDateString(),
                                DateTime.Now.ToLongTimeString(),
                                asset.Name
                            )
                        );
          
                    var task = job.Tasks.AddNew("Encoding task", processor, targetFormat, TaskOptions.ProtectedConfiguration);
                    task.InputAssets.Add(asset);
                    task.OutputAssets.AddNew
                    (
                        string.Format("{0}-Output-{1}", asset.Name, DateTime.Now.ToLongTimeString()),
                        AssetCreationOptions.None
                    );

                    // Capture job status events
                    job.StateChanged += job_StateChanged;

                    // Launch the job
                    job.Submit();

                    Task progressJobTask = job.GetExecutionProgressTask(CancellationToken.None);
                    progressJobTask.Wait();

                    return job;
                });

            return t;
        }

        void job_StateChanged(object sender, JobStateChangedEventArgs e)
        {
            if (this.EncodingJobProgressChanged != null)
            {
                Dictionary<string, string> d = new Dictionary<string, string>();
                d.Add("JobName", ((IJob)sender).Name);
                d.Add("CurrentState", e.CurrentState.ToString());
                d.Add("PreviousState", e.PreviousState.ToString());

                this.EncodingJobProgressChanged(this, d);
            }
        }

        #endregion

        #region Asset Publishing

        public Task<string> PublishAsset(string assetId)
        {
            var t = Task.Run(() =>
            {
                int streamingDays = 1;

                IAsset streamingAsset = _cloudMediaContext.Assets.Where(item => item.Id == assetId).FirstOrDefault();
                IAccessPolicy accessPolicy = _cloudMediaContext.AccessPolicies.Create(streamingAsset.Name, TimeSpan.FromDays(streamingDays),
                                                AccessPermissions.Read | AccessPermissions.List);

                string streamingUrl = string.Empty;
                var assetFiles = streamingAsset.AssetFiles.ToList();

                var streamingAssetFile = assetFiles.Where(f => f.Name.ToLower().EndsWith("m3u8-aapl.ism")).FirstOrDefault();
                if (streamingAssetFile != null)
                {
                    var locator = _cloudMediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, streamingAsset, accessPolicy);
                    Uri hlsUri = new Uri(locator.Path + streamingAssetFile.Name + "/manifest(format=m3u8-aapl)");
                    streamingUrl = hlsUri.ToString();
                }

                streamingAssetFile = assetFiles.Where(f => f.Name.ToLower().EndsWith(".ism")).FirstOrDefault();
                if (string.IsNullOrEmpty(streamingUrl) && streamingAssetFile != null)
                {
                    var locator = _cloudMediaContext.Locators.CreateLocator(LocatorType.OnDemandOrigin, streamingAsset, accessPolicy);
                    Uri smoothUri = new Uri(locator.Path + streamingAssetFile.Name + "/manifest");
                    streamingUrl = smoothUri.ToString();
                }

                streamingAssetFile = assetFiles.Where(f => f.Name.ToLower().EndsWith(".mp4")).FirstOrDefault();
                if (string.IsNullOrEmpty(streamingUrl) && streamingAssetFile != null)
                {
                    var locator = _cloudMediaContext.Locators.CreateLocator(LocatorType.Sas, streamingAsset, accessPolicy);
                    var mp4Uri = new UriBuilder(locator.Path);
                    mp4Uri.Path += "/" + streamingAssetFile.Name;
                    streamingUrl = mp4Uri.ToString();
                }

                return streamingUrl;
            });

            return t;
        }

        #endregion
    }
}
