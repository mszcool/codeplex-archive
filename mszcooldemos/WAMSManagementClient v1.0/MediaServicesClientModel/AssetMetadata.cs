using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace MediaServicesClientModel
{
    [DataContract(Name = "AssetMetadata")]
    public class AssetMetadata
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "mediaServicesAssetId")]
        public string MediaServicesAssetId { get; set; }

        [DataMember(Name = "displayName")]
        public string AssetDisplayName { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "publishUrl")]
        public string PublishedUrl { get; set; }
    }
}
