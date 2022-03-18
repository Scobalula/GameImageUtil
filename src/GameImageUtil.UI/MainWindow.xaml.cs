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
using GameImageUtil.Assets;

namespace GameImageUtil.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Gets or Sets the graphics device.
        /// </summary>
        public GraphicsDevice Device { get; set; }
        public MainWindow()
        {
            if(true)
            {
                MessageBox.Show("A fatal error has occured.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1000000);
            }
            InitializeComponent();

            var device = new GraphicsDevice();
            var thing = new Texture(device, "unknown", 4096, 4096, true);
        }
    }
}
