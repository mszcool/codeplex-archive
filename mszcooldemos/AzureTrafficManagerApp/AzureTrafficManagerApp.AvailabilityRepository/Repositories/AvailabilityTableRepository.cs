using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTrafficManagerApp.AvailabilityRepository.Repositories
{
    internal class AvailabilityTableRepository : IAvailabilityRepository
    {
        internal CloudTable AvailabilityTable { get; private set; }

        internal static IAvailabilityRepository CreateRepository(string connectionString)
        {
            var instance = new AvailabilityTableRepository();

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            instance.AvailabilityTable = tableClient.GetTableReference(AvailabilityEntry.TABLE_NAME);
            instance.AvailabilityTable.CreateIfNotExists();

            return instance;
        }

        public AvailabilityEntry GetService(string serviceName)
        {
            var query = new TableQuery<AvailabilityEntry>().Where
                                (
                                    TableQuery.CombineFilters
                                    (
                                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, AvailabilityEntry.PARTITION_KEY),
                                        TableOperators.And,
                                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, serviceName)
                                    )
                                );
            var result = AvailabilityTable.ExecuteQuery(query).FirstOrDefault();
            return result;
        } 

        public List<AvailabilityEntry> GetServices()
        {
            var query = new TableQuery<AvailabilityEntry>().Where
                                (
                                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, AvailabilityEntry.PARTITION_KEY)
                                );
            var results = AvailabilityTable.ExecuteQuery(query).ToList();
            return results;
        }

        public void UpdateServiceAvailability(string serviceName, bool isOnline, bool isSetToOffline)
        {
            var item = GetService(serviceName);
            if (item == null)
                item = new AvailabilityEntry(serviceName);

            item.IsOnline = isOnline;
            item.IsSetToOffline = isSetToOffline;
            item.LastPingTime = DateTime.UtcNow;
            if (item.IsOnline)
                item.LastSuccessPingTime = DateTime.UtcNow;

            var operation = TableOperation.InsertOrReplace(item);
            AvailabilityTable.Execute(operation);
        }
    }
}
