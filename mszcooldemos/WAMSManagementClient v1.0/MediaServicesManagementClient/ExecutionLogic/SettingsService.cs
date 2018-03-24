using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServicesManagementClient.ExecutionLogic
{
    public interface ISettingsService
    {
        void UpdateSettings(string mediaServiceName, string mediaServiceKey, string mobileServiceUrl, string mobileServiceKey);

        string MediaServiceName { get;  }
        string MediaServiceKey { get; }

        string MobileServiceUrl { get; }
        string MobileServiceKey { get; }
    }

    public class SettingsService : ISettingsService
    {
        private MediaServicesManagementClient.Properties.Settings _wpfAppSettings;

        public SettingsService()
        {
            _wpfAppSettings = MediaServicesManagementClient.Properties.Settings.Default;
        }

        public void UpdateSettings(string mediaServiceUrl, string mediaServiceKey, string mobileServiceUrl, string mobileServiceKey)
        {
            _wpfAppSettings.MediaServiceName = mediaServiceUrl;
            _wpfAppSettings.MediaServiceKey = mediaServiceKey;

            _wpfAppSettings.MobileServiceKey = mobileServiceKey;
            _wpfAppSettings.MobileServiceUrl = mobileServiceUrl;

            _wpfAppSettings.Save();
        }

        public string MediaServiceName
        {
            get { return _wpfAppSettings.MediaServiceName; }
        }

        public string MediaServiceKey
        {
            get { return _wpfAppSettings.MediaServiceKey; }
        }

        public string MobileServiceUrl
        {
            get { return _wpfAppSettings.MobileServiceUrl; }
        }

        public string MobileServiceKey
        {
            get { return _wpfAppSettings.MobileServiceKey; }
        }
    }
}
