using MediaServicesManagementClient.ExecutionLogic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MediaServicesManagementClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MediaServicesManagementClient.App MyApp
        {
            get
            {
                return App.Current as MediaServicesManagementClient.App;
            }
        }

        public ISettingsService SettingsSvc { get; private set; }

        private IAssetMediaService _assetMediaService = null;
        public IAssetMediaService AssetMediaSvc 
        {
            get
            {
                if (_assetMediaService == null)
                    _assetMediaService = new AssetMediaService(SettingsSvc.MediaServiceName, SettingsSvc.MediaServiceKey);
                return _assetMediaService;
            }
        }

        private IAssetMetadataService _assetMetadataService = null;
        public IAssetMetadataService AssetMetadataSvc 
        {
            get
            {
                if (_assetMetadataService == null)
                    _assetMetadataService = new AssetMetadataService(SettingsSvc.MobileServiceUrl, SettingsSvc.MobileServiceKey);
                return _assetMetadataService;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //
            // The settings service must be loaded in any case so that
            // all settings are loaded at the time when the other services 
            // are requested by the UI.
            //
            SettingsSvc = new SettingsService();
        }

        protected override void OnLoadCompleted(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnLoadCompleted(e);
        }
    }
}
