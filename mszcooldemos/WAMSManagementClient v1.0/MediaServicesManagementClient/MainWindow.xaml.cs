using MediaServicesManagementClient.Controllers;
using MediaServicesManagementClient.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace MediaServicesManagementClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainController Controller { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Controller = new MainController();
            this.DataContext = Controller.ViewModel;

            AssetsUI.InitControllerFromParent(Controller);
            JobsUI.InitializeController(Controller);

            Controller.GoOnline();

            Controller.LoadEncodingPresets();
        }

        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            Controller.ShowSettings();
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            Controller.ExitApp();
        }

        private void GoOnlineMenu_Click(object sender, RoutedEventArgs e)
        {
            Controller.GoOnline();
        }

        private void ShowAssetsButton_Click(object sender, RoutedEventArgs e)
        {
            Controller.ShowAssets();
        }

        private void IngestFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "MP4 Files (.mp4)|*.mp4|WMV Files (.wmv)|*.wmv";
            bool? result = dlg.ShowDialog();

            if (result != null || result != false)
                Controller.IngestFile(System.IO.Path.GetFileName(dlg.FileName), dlg.FileName);
        }

        private void IngestDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented yet", "Not Implemented yet", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void IngestBlobButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented yet", "Not implemented yet", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void EncodeButton_Click(object sender, RoutedEventArgs e)
        {
            // Note: all pre-sets can be found here: http://msdn.microsoft.com/en-us/library/jj129582.aspx
            if (Controller.ViewModel.SelectedAsset != null)
                Controller.EncodeSelectedAsset();
            else
                MessageBox.Show("Please select an asset before trying to encode it!", "Select an Asset", MessageBoxButton.OK, MessageBoxImage.Hand);
        }

        private void JobsMonitoringButton_Click(object sender, RoutedEventArgs e)
        {
            Controller.ShowJobs();
        }

        private void JobsDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Controller.ViewModel.SelectedJob != null)
                Controller.DeleteSelectedJob();
            else
                MessageBox.Show("Please select a job before trying to delete it!", "Select a Job", MessageBoxButton.OK, MessageBoxImage.Hand);
        }

        private void PublishAssetButton_Click(object sender, RoutedEventArgs e)
        {
            if (Controller.ViewModel.SelectedAsset != null)
                Controller.PublishSelectedAsset();
            else
                MessageBox.Show("Please select an asset before trying to publish it!", "Select an Asset", MessageBoxButton.OK, MessageBoxImage.Hand);

        }

        private void BrowseProcessorsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented yet", "Not implemented yet", MessageBoxButton.OK, MessageBoxImage.Warning);
            //Controller.ShowProcessors();
        }
    }
}
