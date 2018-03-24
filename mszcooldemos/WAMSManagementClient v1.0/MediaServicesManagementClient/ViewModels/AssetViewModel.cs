using MediaServicesClientModel;
using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServicesManagementClient.ViewModels
{
    public class AssetViewModel : BaseViewModel
    {
        private IAsset _mediaServicesAsset;
        private AssetMetadata _mobileServicesModel;

        public AssetViewModel(IAsset mediaAsset, AssetMetadata assetMetadata)
        {
            _mediaServicesAsset = mediaAsset;
            _mobileServicesModel = assetMetadata;
        }

        public IAsset GetMediaServicesAsset()
        {
            return _mediaServicesAsset;
        }

        public AssetMetadata GetAssetMetadata()
        {
            return _mobileServicesModel;
        }

        #region Additional UI Control helpers

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public bool Modified { get; set; }

        #endregion

        #region Data hold in Windows Azure MOBILE services metadata service

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public int AssetMetadataId
        {
            get { return _mobileServicesModel.Id; }
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public string AssetDisplayName
        {
            get { return _mobileServicesModel.AssetDisplayName; }
            set
            {
                _mobileServicesModel.AssetDisplayName = value;
                base.OnPropertyChanged("AssetDisplayName");
            }
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public string AssetDescription
        {
            get { return _mobileServicesModel.Description; }
            set
            {
                _mobileServicesModel.Description = value;
                base.OnPropertyChanged("AssetDescription");
            }
        }

        [System.ComponentModel.DataAnnotations.Required()]
        public string PublishUrl
        {
            get { return _mobileServicesModel.PublishedUrl; }
            set
            {
                _mobileServicesModel.PublishedUrl = value;
                base.OnPropertyChanged("PublishUrl");
            }
        }

        #endregion

        #region Data from Windows Azure MEDIA services

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public string MediaAssetId
        {
            get { return _mediaServicesAsset.Id; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public string MediaAssetName
        {
            get { return _mediaServicesAsset.Name; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public DateTime MediaAssetCreated
        {
            get { return _mediaServicesAsset.Created; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public DateTime MediaAssetLastModified
        {
            get { return _mediaServicesAsset.LastModified; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public string MediaAssetState
        {
            get { return _mediaServicesAsset.State.ToString(); }
        }

        #endregion
    }
}
