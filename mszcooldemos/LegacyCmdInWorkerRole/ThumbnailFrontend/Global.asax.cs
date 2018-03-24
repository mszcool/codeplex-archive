// <copyright file="Global.asax.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using Microsoft.WindowsAzure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using ThumbnailFrontend;
using ThumbnailShared;

namespace ThumbnailFrontend
{
    public class Global : HttpApplication
    {
        private static ThumbnailQueueRepository _queueRep = null;
        public static ThumbnailQueueRepository QueueRepository
        {
            get
            {
                if (_queueRep == null)
                {
                    var storageConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageAccountConfigurationString);
                    _queueRep = new ThumbnailQueueRepository(storageConnectionString);
                }
                return _queueRep;
            }
        }

        private static ThumbnailJobRepository _jobsRep = null;
        public static ThumbnailJobRepository JobsRepository
        {
            get
            {
                if (_jobsRep == null)
                {
                    var storageConnectionString = CloudConfigurationManager.GetSetting(SharedConstants.StorageAccountConfigurationString);
                    _jobsRep = new ThumbnailJobRepository(storageConnectionString);
                }
                return _jobsRep;
            }
        }


        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AuthConfig.RegisterOpenAuth();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown

        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs

        }
    }
}
