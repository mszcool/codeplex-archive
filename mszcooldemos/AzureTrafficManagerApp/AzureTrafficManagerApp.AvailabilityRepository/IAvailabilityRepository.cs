using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureTrafficManagerApp.AvailabilityRepository
{
    public interface IAvailabilityRepository
    {
        AvailabilityEntry GetService(string serviceName);
        List<AvailabilityEntry> GetServices();
        void UpdateServiceAvailability(string serviceName, bool isOnline, bool isSetToOffline);
    }
}
