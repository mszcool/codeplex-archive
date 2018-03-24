using MediaServicesManagementClient.Controllers;
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
    /// Interaction logic for JobsView.xaml
    /// </summary>
    public partial class JobsView : UserControl
    {
        public JobsView()
        {
            InitializeComponent();
        }

        public MainController Controller { get; private set; }

        internal void InitializeController(Controllers.MainController controller)
        {
            this.Controller = controller;
        }
    }
}
