using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MediaServicesManagementClient.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Data Items

        private SettingsViewModel _settings;
        public SettingsViewModel Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged("Settings");
            }
        }

        private ObservableCollection<AssetViewModel> _assets;
        public ObservableCollection<AssetViewModel> Assets 
        {
            get { return _assets; }
            set { _assets = value; OnPropertyChanged("Assets"); } 
        }

        private ObservableCollection<JobViewModel> _jobs;
        public ObservableCollection<JobViewModel> Jobs 
        {
            get { return _jobs; }
            set { _jobs = value; OnPropertyChanged("Jobs"); } 
        }

        private bool _online;
        public bool Online
        {
            get { return _online; }
            set { _online = value; OnPropertyChanged("Online"); }
        }

        private string _selectedEncodingPreset;
        public string SelectedEncodingPreset
        {
            get { return _selectedEncodingPreset; }
            set { _selectedEncodingPreset = value; OnPropertyChanged("SelectedEncodingPreset"); }
        }

        private ObservableCollection<string> _encodingPresets;
        public ObservableCollection<string> EncodingPresets
        {
            get { return _encodingPresets; }
            set
            {
                _encodingPresets = value;
                OnPropertyChanged("EncodingPresets");
            }
        }

        private AssetViewModel _selectedAsset;
        public AssetViewModel SelectedAsset
        {
            get { return _selectedAsset; }
            set { _selectedAsset = value; OnPropertyChanged("SelectedAsset"); }
        }

        private JobViewModel _selectedJob;
        public JobViewModel SelectedJob
        {
            get { return _selectedJob; }
            set { _selectedJob = value; OnPropertyChanged("SelectedJob"); }
        }

        #endregion

        #region UI Control Items

        public bool AssetsBrowsingEnabled
        {
            get { return Online; }
        }

        public bool IngestContentEnabled
        {
            get { return Online; }
        }

        public bool EncodeAssetsEnabled
        {
            get { return Online && (SelectedAsset != null) && (!string.IsNullOrEmpty(SelectedEncodingPreset)); }
        }

        public bool JobsBrowsingEnabled
        {
            get { return Online; }
        }

        public bool JobDeletionEnabled
        {
            get { return (Online && (SelectedJob != null)); }
        }

        public bool PublishAssetEnabled
        {
            get { return Online && (SelectedAsset != null); }
        }

        public bool MediaProcessorBrowsingEnabled
        {
            get { return Online; }
        }

        private string _statusText;
        public string StatusText
        {
            get { return _statusText; }
            set { _statusText = value; OnPropertyChanged("StatusText"); }
        }

        private Visibility _assetsViewVisibility = Visibility.Hidden;
        public Visibility AssetsViewVisibility 
        {
            get { return _assetsViewVisibility; }
            set { _assetsViewVisibility = value; OnPropertyChanged("AssetsViewVisibility"); }
        }

        private Visibility _jobsViewVisibility = Visibility.Hidden;
        public Visibility JobsViewVisibility 
        {
            get { return _jobsViewVisibility; }
            set { _jobsViewVisibility = value; OnPropertyChanged("JobsViewVisibility"); }
        }

        private Visibility _settingsViewVisibility = Visibility.Hidden;
        public Visibility SettingsViewVisibility
        {
            get { return _settingsViewVisibility; }
            set { _settingsViewVisibility = value; OnPropertyChanged("SettingsViewVisibility"); }
        }

        #endregion

        #region Custom Property Notification Changes Handling

        protected override void OnPropertyChanged(string name)
        {
            base.OnPropertyChanged(name);

            switch (name)
            {
                case "Online":
                    OnPropertyChanged("AssetsBrowsingEnabled");
                    OnPropertyChanged("IngestContentEnabled");
                    OnPropertyChanged("EncodeAssetsEnabled");
                    OnPropertyChanged("JobsBrowsingEnabled");
                    OnPropertyChanged("PublishASsetEnabled");
                    OnPropertyChanged("JobDeletionEnabled");
                    break;

                case "SelectedAsset":
                    OnPropertyChanged("EncodeAssetsEnabled");
                    OnPropertyChanged("PublishASsetEnabled");
                    break;

                case "SelectedJob":
                    OnPropertyChanged("JobDeletionEnabled");
                    break;

                case "SelectedEncodingPreset":
                    OnPropertyChanged("EncodeAssetsEnabled");
                    break;
            }
        }

        #endregion
    }
}
