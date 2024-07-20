using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using System.Windows.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for DownloadManagerControl.xaml
    /// </summary>

    public partial class DownloadManagerControl : UserControl, INotifyPropertyChanged
    {

        
        private ObservableCollection<DownloadItem> downloadItems;
        private DispatcherTimer timer;

        private Settings settings;

        private DatabaseManager db;

        public DatabaseManager DbManager
        {
            get { return db; }
            set { db = value; }
        }


        public DownloadManagerControl()
        {
            InitializeComponent();
            DataContext = this;
            LoadSettings();  // Load settings here as well
            // Initialize the DownloadItems collection
            DownloadItems = new ObservableCollection<DownloadItem>();
            // Start the progress update timer
            StartProgressUpdateTimer();
        }

        public class TextTruncationConverter : IValueConverter
        {
            public int MaxLength { get; set; } = 20; // Default max length

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is string text && text.Length > MaxLength)
                {
                    return text.Substring(0, MaxLength) + "...";
                }
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private void LoadSettings()
        {
            try
            {

                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error reading settings file: {ex.Message}");
                //// Handle exceptions or provide a default settings object
            }
        }

        public void CancelDownload(DownloadItem downloadItem)
        {
            if (downloadItem?.DownloadProcess != null)
            {
                try
                {
                    if (!downloadItem.DownloadProcess.HasExited)
                    {
                        downloadItem.DownloadProcess.Kill();
                        downloadItem.Status = "Canceled";
                    }
                }
                catch (InvalidOperationException)
                {
                    // Handle the situation where the process is no longer available
                }

                downloadItem.DownloadProcess = null;

                // Update UI or database as needed
                db.UpdateDownloadItem(downloadItem);

                // Refresh the DataGrid
                RefreshDataGrid();
            }
        }

        private void OpenPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is DownloadItem selectedDownload)
            {
                string pathToOpen = selectedDownload.Status == "Completed" && !string.IsNullOrEmpty(selectedDownload.AlbumDownloadPath)
                                    ? selectedDownload.AlbumDownloadPath
                                    : System.IO.Path.GetDirectoryName(selectedDownload.DownloadPath);

                if (!string.IsNullOrEmpty(pathToOpen) && Directory.Exists(pathToOpen))
                {
                    OpenFolderPath(pathToOpen);
                }
                else
                {
                    MessageBox.Show("Path is not available or does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void OpenFolderPath(string path)
        {
            // Check if the path is a file or directory
            if (File.Exists(path))
            {
                // If it's a file, open the file's location and select the file
                Process.Start("explorer.exe", $"/select, \"{path}\"");
            }
            else if (Directory.Exists(path))
            {
                // If it's a directory, open the directory
                Process.Start("explorer.exe", path);
            }
            else
            {
                MessageBox.Show("The path does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Adjust the index values based on your DataGrid's columns
            // Assuming "Actions" column is initially at index 2 but should be at index 3
            if (DownloadDataGrid.Columns.Count > 5) // Check if you have enough columns
            {
                DownloadDataGrid.Columns[5].DisplayIndex = 2;
            }
        }

        private void RefreshDataGrid()
        {
            Dispatcher.Invoke(() =>
            {
                var currentItemsSource = DownloadDataGrid.ItemsSource;
                DownloadDataGrid.ItemsSource = null;
                DownloadDataGrid.ItemsSource = currentItemsSource;
            });
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Button cancelButton = sender as Button;
            DownloadItem downloadItem = cancelButton.DataContext as DownloadItem;
            CancelDownload(downloadItem);
        }

        private void LogTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            int pos = textBox.GetCharacterIndexFromPoint(e.GetPosition(textBox), true);
            int lineIndex = textBox.GetLineIndexFromCharacterIndex(pos);
            int lineStart = textBox.GetCharacterIndexFromLineIndex(lineIndex);
            int lineLength = textBox.GetLineLength(lineIndex);
            string lineText = textBox.Text.Substring(lineStart, lineLength);

            // Now, determine if the clicked text is a path
            string path = ExtractPathFromLine(lineText);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }

        private string ExtractPathFromLine(string lineText)
        {
            // Implement logic to extract the path from the line text
            // For example, you can look for "✔ Path:" and extract the path after it
            if (lineText.Contains("Path:"))
            {
                int startIndex = lineText.IndexOf("Path:") + "Path:".Length;
                return lineText[startIndex..].Trim();
            }
            return null;
        }

        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<DownloadItem> DownloadItems
        {
            get { return downloadItems; }
            set
            {
                downloadItems = value;
                OnPropertyChanged(nameof(DownloadItems));
            }
        }

        private double overallProgress;
        public double OverallProgress
        {
            get { return overallProgress; }
            set
            {
                if (overallProgress != value)
                {
                    overallProgress = value;
                    OnPropertyChanged(nameof(OverallProgress));
                }
            }
        }


        private DispatcherTimer progressUpdateTimer;

        private void StartProgressUpdateTimer()
        {
            // Create a new timer
            progressUpdateTimer = new DispatcherTimer();
            progressUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            progressUpdateTimer.Tick += ProgressUpdateTimer_Tick;

            // Start the timer
            progressUpdateTimer.Start();
        }

        private void ProgressUpdateTimer_Tick(object sender, EventArgs e)
        {
            // Check if there are any download items
            if (DownloadItems.Count == 0)
            {
                // Stop the progress update timer
                progressUpdateTimer.Stop();
                return;
            }

            // Calculate the overall progress
            double overallProgress = 0;
            int completedItemCount = 0;

            foreach (var downloadItem in DownloadItems)
            {
                // Skip items that are already completed
                if (downloadItem.IsCompleted)
                {
                    completedItemCount++;
                    continue;
                }

                if (downloadItem.Status == "Completed")
                {
                    downloadItem.DownloadProgress = 100;
                    downloadItem.IsCompleted = true;
                    completedItemCount++;
                }
                else if (downloadItem.Status == "In progress" && downloadItem.Output != null)
                {
                    // Try to extract the progress information from the output
                    if (TryExtractProgress(downloadItem.Output, out int currentIndex, out int totalCount))
                    {
                        // Calculate the progress percentage
                        double progressPercentage = (double)currentIndex / totalCount * 100;

                        // Update the download item's progress
                        downloadItem.DownloadProgress = progressPercentage;

                        // Check if the download is completed
                        if (currentIndex == totalCount)
                        {
                            downloadItem.IsCompleted = true;
                            completedItemCount++;
                        }
                    }
                }

                overallProgress += downloadItem.DownloadProgress;
            }

            // Calculate the overall progress percentage
            overallProgress /= DownloadItems.Count - completedItemCount;
            OverallProgress = overallProgress;

            // Check if all downloads are completed
            if (completedItemCount == DownloadItems.Count)
            {
                // Stop the progress update timer
                progressUpdateTimer.Stop();
            }
        }


        private bool TryExtractProgress(string output, out int currentIndex, out int totalCount)
        {
            currentIndex = 0;
            totalCount = 0;

            int startIndex = output.IndexOf("(") + 1;
            int endIndex = output.IndexOf("/");
            if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
            {
                string progress = output.Substring(startIndex, endIndex - startIndex).Trim();
                string[] progressParts = progress.Split('/');
                if (progressParts.Length == 2 && int.TryParse(progressParts[0], out currentIndex) && int.TryParse(progressParts[1], out totalCount))
                {
                    return true;
                }
            }

            return false;
        }


        // Implement INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddDownloadItem(DownloadItem downloadItem)
        {
            if (downloadItem != null && !string.IsNullOrEmpty(downloadItem.Name))
            {
                // Retrieve the download status from the database
                DownloadItem existingItem = db.GetDownloadItem(downloadItem.Id);

                if (existingItem != null)
                {
                    // Update the status and other properties of the existing download item
                    existingItem.Status = downloadItem.Status;
                    existingItem.EndTime = downloadItem.EndTime;
                    existingItem.Output = downloadItem.Output;
                    existingItem.ErrorMessage = downloadItem.ErrorMessage;

                    // Update the existing download item in the database
                    db.UpdateDownloadItem(existingItem);

                    // Find the index of the existing download item in the DownloadItems collection
                    int index = DownloadItems.IndexOf(existingItem);
                    downloadItem.DownloadProgress = 0; // Initialize the DownloadProgress property
                    downloadItem.IsCompleted = false; // Initialize the IsCompleted property
                    if (index >= 0)
                    {
                        // Update the existing download item in the DownloadItems collection
                        DownloadItems[index] = existingItem;
                    }
                }
                else
                {
                    // Add the new download item to the DownloadItems collection
                    DownloadItems.Add(downloadItem);

                    // Save the download item to the database
                    db.SaveDownloadItem(downloadItem);
                }
            }
        }

        public void RemoveDownloadItem(DownloadItem downloadItem)
        {
            DownloadItems.Remove(downloadItem);
        }

        private DownloadItem activeDownloadItem;
        public DownloadItem ActiveDownloadItem
        {
            get { return activeDownloadItem; }
            set
            {
                if (activeDownloadItem != value)
                {
                    activeDownloadItem = value;
                    OnPropertyChanged(nameof(ActiveDownloadItem));
                }
            }
        }

        private void DataGridRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                if (row.DetailsVisibility == Visibility.Collapsed)
                {
                    row.DetailsVisibility = Visibility.Visible;
                }
                else
                {
                    row.DetailsVisibility = Visibility.Collapsed;
                }
            }
        }

        public void UpdateDownloadItemProgress(DownloadItem downloadItem, int currentIndex, int totalCount)
        {
            // Update the progress of the download item
            downloadItem.Progress = currentIndex;

            // Update the overall progress of the download item
            downloadItem.OverallProgress = (double)currentIndex / totalCount;

            // Raise the PropertyChanged event for the updated properties
            OnPropertyChanged(nameof(DownloadItem.Progress));
            OnPropertyChanged(nameof(DownloadItem.OverallProgress));
        }


        private OutputWindow outputWindow;

        private Dictionary<DownloadItem, OutputWindow> openOutputWindows = new Dictionary<DownloadItem, OutputWindow>();

        private void OutputButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DownloadItem downloadItem = button.DataContext as DownloadItem;

            if (openOutputWindows.TryGetValue(downloadItem, out OutputWindow existingWindow))
            {
                // If the window is already open, bring it to the front
                existingWindow.Activate();
            }
            else
            {
                // If the window is not open, create and open a new OutputWindow
                OutputWindow outputWindow = new OutputWindow();
                outputWindow.DownloadItem = downloadItem; // Set the DownloadItem
                outputWindow.Closed += (s, args) =>
                {
                    // Remove the window reference from the dictionary when it's closed
                    openOutputWindows.Remove(downloadItem);
                };
                outputWindow.Show();

                // Store a reference to the open OutputWindow for this DownloadItem
                openOutputWindows[downloadItem] = outputWindow;
            }
        }

        public class StatusToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                string status = value as string;
                string expectedStatus = parameter as string;

                if (status == expectedStatus)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private void OutputWindow_Closed(object sender, EventArgs e)
        {
            // Clear the output and reset the reference to null when the window is closed
            outputWindow.OutputText = string.Empty;
            outputWindow = null;
        }

        public void UpdateOutput(string output)
        {
            if (outputWindow != null && outputWindow.IsVisible)
            {
                outputWindow.Dispatcher.Invoke(() =>
                {
                    outputWindow.OutputTextBox.AppendText(output + Environment.NewLine);
                    outputWindow.OutputTextBox.ScrollToEnd(); // Scroll to the end to show the latest output
                });
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = DownloadItems.Count - 1; i >= 0; i--)
            {
                var item = DownloadItems[i];
                if (item.Status == "Completed" || item.Status == "Failed")
                {
                    DownloadItems.RemoveAt(i);
                }
            }
        }

        //private void CloseButton_Click(object sender, RoutedEventArgs e)
        //{
        //    this.Hide();
        //}

        //private void Window_Closing(object sender, CancelEventArgs e)
        //{
        //    e.Cancel = true;
        //    this.Hide();
        //}
    }
}
