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
using Geres.Samples.ThumbnailGeneratorClient.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Geres.Samples.ThumbnailGeneratorClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainController _Controller = new MainController();

        public MainWindow()
        {
            InitializeComponent();

            //
            // Set the current data context to the controller's view model
            // 
            MainGrid.DataContext = _Controller.ViewModel;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await _Controller.Connect();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _Controller.ScheduleJobs();
        }
    }
}
