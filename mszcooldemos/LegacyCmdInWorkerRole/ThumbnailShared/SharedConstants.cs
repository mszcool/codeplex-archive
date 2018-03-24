// <copyright file="SharedConstants.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThumbnailShared
{
    public sealed class SharedConstants
    {
        public const string StorageAccountConfigurationString = "primaryStorageConnectionString";
        public const string LocalStorageForImageProcessingName = "localImageProcessingTemp";

        public const string JobsTableName = "thumbnailjobstable";
        public const string JobsQueueName = "thumbnailjobsqueue";

        public const string ResultsBlobContainer = "thumbnailresults";
    }
}
