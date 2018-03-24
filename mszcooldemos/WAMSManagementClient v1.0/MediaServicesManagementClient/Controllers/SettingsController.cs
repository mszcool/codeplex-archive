using MediaServicesManagementClient.ExecutionLogic;
using MediaServicesManagementClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServicesManagementClient.Controllers
{
    public class SettingsController
    {
        public ISettingsService SettingsService { get; private set; }
        public SettingsViewModel ViewModel { get; private set; }

        public SettingsController()
        {
            ViewModel = new SettingsViewModel();
            SettingsService = App.MyApp.SettingsSvc;
        }

        public void LoadSettings()
        {
            ViewModel.MediaServicesKey = SettingsService.MediaServiceKey;
            ViewModel.MediaServicesName = SettingsService.MediaServiceName;
            ViewModel.MobileServicesKey = SettingsService.MobileServiceKey;
            ViewModel.MobileServicesUrl = SettingsService.MobileServiceUrl;
        }

        public void SaveSettings()
        {
            SettingsService.UpdateSettings
                (
                    ViewModel.MediaServicesName,
                    ViewModel.MediaServicesKey,
                    ViewModel.MobileServicesUrl,
                    ViewModel.MobileServicesKey
                );
        }
    }
}
