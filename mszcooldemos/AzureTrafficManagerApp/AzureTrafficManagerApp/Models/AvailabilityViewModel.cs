using AzureTrafficManagerApp.AvailabilityRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureTrafficManagerApp.Models
{
    public class AvailabilityViewModel
    {
        public string Location { get; set; }
        public string BackgroundColor { get; set; }
        public List<AvailabilityEntry> ServiceStates { get; set; }
    }
}