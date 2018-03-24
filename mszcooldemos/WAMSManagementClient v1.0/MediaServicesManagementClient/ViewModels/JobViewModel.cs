using Microsoft.WindowsAzure.MediaServices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServicesManagementClient.ViewModels
{
    public class JobViewModel : BaseViewModel
    {
        private IJob _mediaServicesJob;

        public JobViewModel(IJob mediaServicesJob)
        {
            _mediaServicesJob = mediaServicesJob;
        }

        public IJob GetCurrentMediaServicesJob()
        {
            return _mediaServicesJob;
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public string JobId
        {
            get { return _mediaServicesJob.Id; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public string JobName
        {
            get { return _mediaServicesJob.Name; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public DateTime? StartTime
        {
            get { return _mediaServicesJob.StartTime; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public DateTime? EndTime
        {
            get { return _mediaServicesJob.EndTime; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public TimeSpan Duration
        {
            get { return _mediaServicesJob.RunningDuration; }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public bool Completed
        {
            get { return (_mediaServicesJob.State == JobState.Finished); }
        }

        [System.ComponentModel.DataAnnotations.Editable(false)]
        public string Status
        {
            get { return _mediaServicesJob.State.ToString(); }
        }
    }
}
