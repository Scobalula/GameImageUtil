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
using System.Windows.Shapes;

namespace CoDImageUtil
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        /// <summary>
        /// Whether or not we have cancelled
        /// </summary>
        private bool HasCancelled = false;

        /// <summary>
        /// Whether or not the task is complete
        /// </summary>
        private bool Complete = false;

        public ProgressWindow()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void TaskButtonClick(object sender, RoutedEventArgs e)
        {
            if(!HasCancelled && !Complete)
            {
                HasCancelled = true;
                TaskButton.Content = "Cancelling";
            }
            else
            {
                if(Complete)
                {
                    Close();
                }
            }
        }

        public void SetProgressCount(double value)
        {
            // Invoke dispatcher to update UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Progress.Maximum = value;
                Progress.Value = 0;
            }));
        }

        public void SetProgressMessage(string value)
        {
            // Invoke dispatcher to update UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ProgressMessage.Content = value;
            }));
        }

        public bool ProgressComplete()
        {
            // Invoke dispatcher to update UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Progress.Value = Progress.Maximum;
                Complete = true;
                TaskButton.Content = "Close";
            }));

            // Return whether we've cancelled or not
            return HasCancelled;
        }

        /// <summary>
        /// Update Progress and checks for cancel
        /// </summary>
        public bool IncrementProgress()
        {
            // Invoke dispatcher to update UI
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Progress.Value++;
            }));

            // Return whether we've cancelled or not
            return HasCancelled;
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!Complete)
            {
                e.Cancel = true;
            }
        }
    }
}
