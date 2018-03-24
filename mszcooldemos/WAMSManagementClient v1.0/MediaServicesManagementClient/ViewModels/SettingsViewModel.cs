using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServicesManagementClient.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private string _mediaServicesName;
        public string MediaServicesName
        {
            get { return _mediaServicesName; }
            set
            {
                _mediaServicesName = value;
                OnPropertyChanged("MediaServicesUrl");
            }
        }

        private string _mediaServicesKey;
        public string MediaServicesKey
        {
            get { return _mediaServicesKey; }
            set
            {
                _mediaServicesKey = value;
                OnPropertyChanged("MediaServicesKey");
            }
        }

        private string _mobileServicesUrl;
        public string MobileServicesUrl
        {
            get { return _mobileServicesUrl; }
            set
            {
                _mobileServicesUrl = value;
                OnPropertyChanged("MobileServicesUrl");
            }
        }

        private string _mobileServicesKey;
        public string MobileServicesKey
        {
            get { return _mobileServicesKey; }
            set
            {
                _mobileServicesKey = value;
                OnPropertyChanged("MobileServicesKey");
            }
        }
    }
}
