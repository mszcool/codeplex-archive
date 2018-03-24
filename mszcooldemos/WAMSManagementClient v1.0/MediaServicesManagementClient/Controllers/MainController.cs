using MediaServicesManagementClient.ExecutionLogic;
using MediaServicesManagementClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace MediaServicesManagementClient.Controllers
{
    public class MainController
    {
        public MainViewModel ViewModel { get; private set; }
        public IAssetMediaService MediaServiceLogic { get; private set; }
        public IAssetMetadataService MetadataServiceLogic { get; private set; }

        private Dispatcher CurrentDispatcher;

        public MainController()
        {
            ViewModel = new MainViewModel();

            MediaServiceLogic = App.MyApp.AssetMediaSvc;
            MetadataServiceLogic = App.MyApp.AssetMetadataSvc;

            MediaServiceLogic.IngestFileUploadProgressChanged += MediaServiceLogic_IngestFileUploadProgressChanged;
            MediaServiceLogic.EncodingJobProgressChanged += MediaServiceLogic_EncodingJobProgressChanged;

            CurrentDispatcher = Dispatcher.CurrentDispatcher;
        }

        public void ExitApp()
        {
            App.Current.Shutdown(0);
        }

        public void GoOnline()
        {
            try 
            {
                var settingsService = App.MyApp.SettingsSvc;

                MediaServiceLogic.InitMediaContext(settingsService.MediaServiceName, settingsService.MediaServiceKey);
                MetadataServiceLogic.InitMobileServiceContext(settingsService.MobileServiceUrl, settingsService.MobileServiceKey);

                ViewModel.Online = true;
                ViewModel.StatusText = "Online!!";
            }
            catch (Exception ex)
            {
                ViewModel.Online = false;
                ViewModel.StatusText = string.Format("Cannot go online: {0}", ex.Message);
            }
        }

        #region Visibility and UI-control Functions

        public void ShowNothing()
        {
            ViewModel.AssetsViewVisibility = System.Windows.Visibility.Hidden;
            ViewModel.JobsViewVisibility = System.Windows.Visibility.Hidden;
            ViewModel.SettingsViewVisibility = System.Windows.Visibility.Hidden;
        }

        public void ShowSettings()
        {
            ViewModel.AssetsViewVisibility = System.Windows.Visibility.Hidden;
            ViewModel.JobsViewVisibility = System.Windows.Visibility.Hidden;
            ViewModel.SettingsViewVisibility = System.Windows.Visibility.Visible;
        }

        public void ShowAssets()
        {
            ViewModel.AssetsViewVisibility = System.Windows.Visibility.Visible;
            ViewModel.JobsViewVisibility = System.Windows.Visibility.Hidden;
            ViewModel.SettingsViewVisibility = System.Windows.Visibility.Hidden;

            this.LoadAssets();
        }

        public void ShowJobs()
        {
            ViewModel.AssetsViewVisibility = System.Windows.Visibility.Hidden;
            ViewModel.JobsViewVisibility = System.Windows.Visibility.Visible;
            ViewModel.SettingsViewVisibility = System.Windows.Visibility.Hidden;

            this.LoadJobs();
        }

        #endregion

        #region Loading Assets and saving metadata in mobile services

        public void LoadEncodingPresets()
        {
            ViewModel.EncodingPresets = new ObservableCollection<string>(MediaServiceLogic.GetEncodingPresets());
            ViewModel.SelectedEncodingPreset = ViewModel.EncodingPresets.First();
        }

        public async void LoadAssets()
        {
            var results = new ObservableCollection<AssetViewModel>();

            // First get the data from media services
            var mediaServiceAssets = await MediaServiceLogic.GetExistingAssets();

            // For each media services asset create a viewmodel and try to query the metadata
            foreach (var asset in mediaServiceAssets)
            {
                var metadataFromMobileService = await MetadataServiceLogic.GetMetadataByMediaId(asset.Id);
                var metadataFromMobileServiceItem = metadataFromMobileService.FirstOrDefault();
                if (metadataFromMobileServiceItem == null)
                    metadataFromMobileServiceItem = new MediaServicesClientModel.AssetMetadata() { MediaServicesAssetId = asset.Id };

                // Create the ViewModel
                results.Add
                    (
                        new AssetViewModel(asset, metadataFromMobileServiceItem)
                    );
            }

            // Finally bind the item to the UI
            ViewModel.Assets = results;
        }

        public void SaveAssetMetadata(AssetViewModel itemToUpdate)
        {
            MetadataServiceLogic.UpdateAssetMetadata(itemToUpdate.GetAssetMetadata());
        }

        #endregion

        #region Ingest files and directories

        public async void IngestFile(string assetName, string fileName)
        {
            var fileExt = System.IO.Path.GetExtension(fileName);
            var mimeType = string.Format("video/{0}", fileExt.Substring(1));

            ViewModel.StatusText = "Start uploading file...";

            await MediaServiceLogic.IngestFile(assetName, fileName, mimeType);

            LoadAssets();
        }

        void MediaServiceLogic_IngestFileUploadProgressChanged(object sender, Dictionary<string, string> e)
        {
            CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                UpdateViewModelStatusTextByDictionary("Uploading file... ", e);
            }));
        }

        #endregion

        #region Encoding Assets

        public async void EncodeSelectedAsset()
        {
            if (ViewModel.SelectedAsset != null)
            {
                ViewModel.StatusText = string.Format("Start encoding asset {0} / {1}", ViewModel.SelectedAsset.MediaAssetId, ViewModel.SelectedAsset.MediaAssetName);
 
                await MediaServiceLogic.EncodeAsset(
                            ViewModel.SelectedAsset.MediaAssetId, 
                            ViewModel.SelectedEncodingPreset);
            }
            else
            {
                throw new InvalidOperationException("No asset view model selected!");
            }
        }

        void MediaServiceLogic_EncodingJobProgressChanged(object sender, Dictionary<string, string> e)
        {
            CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                UpdateViewModelStatusTextByDictionary(string.Format("Job update at {0}", DateTime.Now.ToLongTimeString()), e);
            }));

        }

        #endregion

        #region Job Monitoring

        public async void LoadJobs()
        {
            // Get the jobs through the business logic component
            var jobs = await MediaServiceLogic.GetAllJobs();

            // Next convert the jobs into the ViewModel with a ObservableCollection
            var jobsList = (from j in jobs
                            select new JobViewModel(j)).ToList();

            // Update the ViewModel with the Jobs-List
            ViewModel.Jobs = new ObservableCollection<JobViewModel>(jobsList);
        }

        #endregion

        #region Publishing Assets

        public async void PublishSelectedAsset()
        {
            if (ViewModel.SelectedAsset == null)
                throw new InvalidOperationException("SelectedAsset is null, please select an asset through ViewModel");

            // Publish the asset via media services
            var publishUrl = await MediaServiceLogic.PublishAsset(ViewModel.SelectedAsset.MediaAssetId);

            // Update the publish URL for the mobile services metadata
            ViewModel.SelectedAsset.PublishUrl = publishUrl;
            MetadataServiceLogic.UpdateAssetMetadata(ViewModel.SelectedAsset.GetAssetMetadata());
        }

        #endregion
        
        #region Private Helper Methods

        private void UpdateViewModelStatusTextByDictionary(string prefixText, Dictionary<string, string> e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(prefixText);
            foreach (var key in e.Keys)
            {
                sb.AppendFormat(" {0}={1} ", key, e[key]);
            }
            ViewModel.StatusText = sb.ToString();
        }

        #endregion

        #region Not Implemented, yet
        
        internal void ShowProcessors()
        {
            throw new NotImplementedException();
        }

        internal void DeleteSelectedJob()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
