using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeresAutoscalerPolicySamples
{
    public static class Constants
    {
        public const string CONFIG_MAXIMUM_JOBHOSTS = "Geres.AutoScaler.PolicySamples.MaximumJobHosts";
        public const string CONFIG_MINIMUM_RUNNING_JOBHOSTS = "Geres.AutoScaler.PolicySamples.MinimumRunningJobHosts";
        public const string CONFIG_MAXIMUM_IDLE_JOBHOSTS = "Geres.AutoScaler.PolicySamples.MaximumIdleJobHosts";
        public const string CONFIG_MAXIMUM_IDLE_TIME_IN_MIN = "Geres.AutoScaler.PolicySamples.IdleTimeInMinutes";
    }
}
