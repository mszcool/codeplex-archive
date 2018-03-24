using MediaServicesManagementClient.ViewModels;
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

namespace MediaServicesManagementClient.Controls
{
    /// <summary>
    /// Interaction logic for AssetView.xaml
    /// </summary>
    public partial class AssetView : UserControl
    {
        public AssetView()
        {
            InitializeComponent();
        }

        public Controllers.MainController Controller { get; private set; }

        public void InitControllerFromParent(Controllers.MainController controller)
        {
            this.Controller = controller;
            //this.DataContext = controller.ViewModel;
        }

        private void AssetsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // Saves the current metadata back to the mobile service
            var editedItem = e.Row.Item as AssetViewModel;
            editedItem.Modified = true;
        }

        private void AssetsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 1)
            {
                var vm = e.RemovedItems[0] as AssetViewModel;
                if (vm.Modified) Controller.SaveAssetMetadata(vm);
            }
        }
    }
}
