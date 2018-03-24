// <copyright file="ThumbnailBlobRepository.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThumbnailShared
{
    public class ThumbnailBlobRepository
    {
        private CloudBlobContainer _thumbnailsContainer;

        public ThumbnailBlobRepository(string storageConnectionString)
        {
            var cloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var blobClient = cloudStorageAccount.CreateCloudBlobClient();
            _thumbnailsContainer = blobClient.GetContainerReference(SharedConstants.ResultsBlobContainer);
            _thumbnailsContainer.CreateIfNotExists();
        }

        public string SaveImageToContainer(System.IO.Stream sourceStream, string blobName)
        {
            var blobRef = _thumbnailsContainer.GetBlockBlobReference(blobName);
            blobRef.UploadFromStream(sourceStream);
            return blobRef.Uri.ToString();
        }
    }
}
