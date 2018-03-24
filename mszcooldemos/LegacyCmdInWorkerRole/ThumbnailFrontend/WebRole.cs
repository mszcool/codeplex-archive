// <copyright file="WebRole.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace ThumbnailFrontend
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
