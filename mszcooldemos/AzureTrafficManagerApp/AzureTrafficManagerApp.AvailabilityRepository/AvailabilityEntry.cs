using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureTrafficManagerApp.AvailabilityRepository
{
    public class AvailabilityEntry : TableEntity
    {
        public const string TABLE_NAME = "availabilityTable";
        public const string PARTITION_KEY = "AzureTrafficManagerApp.Availability.Partition";

        public AvailabilityEntry()
        {
        }

        public AvailabilityEntry(string serviceName)
        {
            PartitionKey = PARTITION_KEY;
            RowKey = serviceName;
        }

        [IgnoreProperty]
        public string ServiceName { get { return RowKey; } set { RowKey = value; } }

        public bool IsOnline { get; set; }

        public bool IsSetToOffline { get; set; }
        
        public DateTime LastPingTime { get; set; }

        public DateTime? LastSuccessPingTime { get; set; }
    }
}
