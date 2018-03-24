// <copyright file="ThumbnailQueueRepository.cs" company="Personal">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <author>Mario Szpuszta</author>
// <date>2013-8-7, 10:44</date>
// <summary>This is a sample and demo - use it at your full own risk!</summary>
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThumbnailShared
{
    public class ThumbnailQueueRepository
    {
        private CloudQueue _jobsQueue;

        public ThumbnailQueueRepository(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _jobsQueue = queueClient.GetQueueReference(SharedConstants.JobsQueueName);
            _jobsQueue.CreateIfNotExists();
        }

        public void SubmitJob(string jobId)
        {
            var message = new CloudQueueMessage(jobId);
            _jobsQueue.AddMessage(message);
        }

        public CloudQueueMessage GetMessageForJob(out string jobId, out bool dequeued)
        {
            var message = _jobsQueue.GetMessage(TimeSpan.FromMinutes(2));
            if (message == null)
            {
                jobId = string.Empty;
                dequeued = false;
                return null;
            }

            if (message.DequeueCount > 3)
            {
                // Remove the poison message from the queue
                _jobsQueue.DeleteMessage(message);

                jobId = message.AsString;
                dequeued = true;
            }
            else
            {
                // Return the job ID
                jobId = message.AsString;
                dequeued = false;
            }

            return message;
        }

        public void DeleteMessage(CloudQueueMessage message)
        {
            _jobsQueue.DeleteMessage(message);
        }
    }
}
