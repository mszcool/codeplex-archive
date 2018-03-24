using MediaServicesClientModel;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServicesManagementClient.ExecutionLogic
{
    public interface IAssetMetadataService
    {
        Task<AssetMetadata> GetMetadataById(int id);
        Task<IList<AssetMetadata>> GetMetadataByMediaId(string mediaServicesAssetId);

        void InsertNewAssetMetadata(AssetMetadata newAsset);
        void UpdateAssetMetadata(AssetMetadata existingAsset);
        void DeleteAssetMetadata(AssetMetadata existingAsset);

        void InitMobileServiceContext();
        void InitMobileServiceContext(string appUrl, string appKey);
    }

    public class AssetMetadataService : IAssetMetadataService
    {
        private string _appUrl, _appKey;

        private IMobileServiceClient _myMetaDataMobileServiceClient;
        private IMobileServiceTable<AssetMetadata> _assetMetadataTable;

        public AssetMetadataService(string appUrl, string appKey)
        {
            _appUrl = appUrl;
            _appKey = appKey;
        }

        public void InitMobileServiceContext()
        {
            InitMobileServiceContext(_appUrl, _appKey);
        }

        public void InitMobileServiceContext(string appUrl, string appKey)
        {
            _myMetaDataMobileServiceClient = new MobileServiceClient(appUrl, appKey);
            _assetMetadataTable = _myMetaDataMobileServiceClient.GetTable<AssetMetadata>();
        }

        public async Task<AssetMetadata> GetMetadataById(int id)
        {
            var assetMetadata = await _assetMetadataTable.LookupAsync(id); ;
            return assetMetadata;
        }

        public async Task<IList<AssetMetadata>> GetMetadataByMediaId(string mediaServicesAssetId)
        {
            var assetMetadata = await _assetMetadataTable.Where(m => m.MediaServicesAssetId == mediaServicesAssetId).ToListAsync();
            return assetMetadata;
        }

        public async void InsertNewAssetMetadata(AssetMetadata newAsset)
        {
            FixNullValues(newAsset);

            await _assetMetadataTable.InsertAsync(newAsset);
        }

        public async void UpdateAssetMetadata(AssetMetadata existingAsset)
        {
            FixNullValues(existingAsset);

            if (existingAsset.Id <= 0)
                await _assetMetadataTable.InsertAsync(existingAsset);
            else
                await _assetMetadataTable.UpdateAsync(existingAsset);
        }

        public async void DeleteAssetMetadata(AssetMetadata existingAsset)
        {
            await _assetMetadataTable.DeleteAsync(existingAsset);
        }

        #region Private Helpers

        private void FixNullValues(AssetMetadata existingAsset)
        {
            if (existingAsset.PublishedUrl == null) existingAsset.PublishedUrl = string.Empty;
            if (existingAsset.Description == null) existingAsset.Description = string.Empty;
            if (existingAsset.AssetDisplayName == null) existingAsset.AssetDisplayName = string.Empty;
        }

        #endregion
    }
}
