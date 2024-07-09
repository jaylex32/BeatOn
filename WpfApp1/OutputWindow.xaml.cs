using System;
using System.Windows;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class OutputWindow : Window
    {
        private DispatcherTimer timer;
        private DownloadItem downloadItem;

        // Add a new dependency property for OutputText
        public static readonly DependencyProperty OutputTextProperty = DependencyProperty.Register(
            "OutputText", typeof(string), typeof(OutputWindow), new PropertyMetadata(""));

        public string OutputText
        {
            get { return (string)GetValue(OutputTextProperty); }
            set { SetValue(OutputTextProperty, value); }
        }

        public OutputWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Create the timer and set the interval to 3 seconds
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick;
        }

        // Event handler for the timer tick event
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Update the log text in the OutputWindow
            OutputText = downloadItem.Log;
        }

        // Property to set the DownloadItem and start the timer when the window is shown
        public DownloadItem DownloadItem
        {
            get { return downloadItem; }
            set
            {
                downloadItem = value;
                // Start the timer when the window is shown
                timer.Start();
            }
        }

        // Event handler for the Closed event of the OutputWindow to stop and dispose of the timer
        private void OutputWindow_Closed(object sender, EventArgs e)
        {
            timer.Stop();
            timer.Tick -= Timer_Tick; // Remove the event handler to avoid potential memory leaks
        }
    }
}

