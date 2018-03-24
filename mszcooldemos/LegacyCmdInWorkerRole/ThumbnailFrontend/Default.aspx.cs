// <copyright file="Default.aspx.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ThumbnailFrontend
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void StartProcessingButton_Click(object sender, EventArgs e)
        {
            try
            {
                var jobsRep = Global.JobsRepository;
                var queueRep = Global.QueueRepository;

                // File a new job in the table and submit into the queue for the worker to be processed
                var jobId = jobsRep.InsertNewJob(SourceImageUrlText.Text, TargetImageNameText.Text);
                queueRep.SubmitJob(jobId);

                StatusLabel.Text = "Successfully submitted new job: " + jobId;
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Unable to submit job due to an exception: " + ex.Message;
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}