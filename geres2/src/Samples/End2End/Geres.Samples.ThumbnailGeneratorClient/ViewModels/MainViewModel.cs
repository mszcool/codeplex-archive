//
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//           http://www.apache.org/licenses/LICENSE-2.0 
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geres.Samples.ThumbnailGeneratorClient.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged("IsConnected");
            }
        }

        private bool _useDefaultBatch;
        public bool UseDefaultBatch 
        {
            get { return _useDefaultBatch; }
            set
            {
                _useDefaultBatch = value;
                OnPropertyChanged("UseDefaultBatch");
                OnPropertyChanged("UseCustomBatch");
            }
        }

        public bool UseCustomBatch
        {
            get { return !UseDefaultBatch; }
        }

        private string _batchName;
        public string BatchName 
        {
            get { return _batchName; }
            set
            {
                _batchName = value;
                OnPropertyChanged("BatchName");
            }
        }

        private string _sourceBlobContainerName;
        public string SourceBlobContainerName 
        {
            get { return _sourceBlobContainerName; }
            set
            {
                _sourceBlobContainerName = value;
                OnPropertyChanged("SourceBlobContainerName");
            }
        }

        private string _targetBlobContainerName;
        public string TargetBlobContainerName 
        {
            get { return _targetBlobContainerName; }
            set
            {
                _targetBlobContainerName = value;
                OnPropertyChanged("TargetBlobContainerName");
            }
        }

        private bool _allJobsCompleted;
        public bool AllJobsCompleted
        {
            get { return _allJobsCompleted; }
            set
            {
                _allJobsCompleted = value;
                OnPropertyChanged("AllJobsCompleted");
            }
        }

        public ObservableCollection<string> ClientLogActions { get; set; }
        public ObservableCollection<string> NotificationMessages { get; set; }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}
