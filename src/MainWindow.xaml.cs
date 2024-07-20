using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using static WpfApp1.MainWindow;
using System.Security.Principal;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DatabaseManager db;
        private DatabaseManager databaseManager;
        private Settings settings;
        public event Action<string> DownloadOutputAvailable;
        private int activeDownloads = 0;
       
        private System.Windows.Controls.ContextMenu trayIconContextMenu;
        private System.Windows.Controls.MenuItem openMenuItem;
        private System.Windows.Controls.MenuItem exitMenuItem;
        private bool isMinimizedToTray = false;
        private TaskbarIcon taskbarIcon;
        public class SearchResult
        {
            public string Name { get; set; }
            public string Link { get; set; }
            public string Picture { get; set; }
            public string Title { get; set; }
            public string Genre { get; set; }
            public string Albums { get; set; }
            public string QobuzUrl { get; set; }
            public string QobuzPoster { get; set; }
            public string Qposterlabel { get; set; }
            public string Genrelabel { get; set; }
            public string Songs { get; set; }
            public string SongsPreview { get; set; }
            public string albumId { get; set; }
            public string artistId { get; set; }
            public string ArtistId { get; set; }
        }

        private List<SearchResult> artistList = new List<SearchResult>();
        private List<SearchResult> albumsList = new List<SearchResult>();
        private List<SearchResult> songsList = new List<SearchResult>();
        private List<SearchResult> playlistList = new List<SearchResult>();
        private List<SearchResult> Qobuz_Artist_List = new List<SearchResult>();
        private List<SearchResult> Qobuz_Songs_List = new List<SearchResult>();
        private List<SearchResult> Qobuz_Albums_List = new List<SearchResult>();
        private List<SearchResult> Qobuz_Playlist_List = new List<SearchResult>();
        private DownloadManagerWindow downloadManagerWindow;
        private string searchType = ""; // Declare the searchType variable here
        private SemaphoreSlim downloadSemaphore;
        private SemaphoreSlim lidarrDownloadSemaphore;
        private LidarrDownloader lidarrDownloader;
        private WebUIServer _webUIServer;
        private DispatcherTimer downloadTimer;

        public MainWindow()
        {
            InitializeComponent();
            UpdateDownloadBadge();
            // Instantiate your DatabaseManager
            DatabaseManager dbManager = new DatabaseManager();
            db = new DatabaseManager();
            
            // Check if downloadManagerControl is not null before assigning DbManager
            if (downloadManagerControl != null)
            {
                downloadManagerControl.DbManager = dbManager;
            }
            else
            {
                // Handle the case where downloadManagerControl is null
               
            }

            // Initialize the settings object
            settings = new Settings();

            LoadSettings();

            // Check for administrative privileges
            if (settings.WebUIEnabled && !IsRunningAsAdmin())
            {
                MessageBoxResult result = MessageBox.Show("BeatOn Dashboard requires administrative privileges to run. Would you like to restart the application with administrative privileges?", "Administrative Privileges Required", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    RestartAsAdmin();
                    Application.Current.Shutdown(); // Close the current instance
                    return;
                }
                else
                {
                    settings.WebUIEnabled = false; // Disable WebUI if the user chooses not to restart
                }
            }

            var lidarrDownloaderStats = new LidarrDownloaderStats();
            try
            {
                InitializeDependencies(lidarrDownloaderStats);
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error in MainWindow constructor: {ex.Message}");
                
            }

            // Set the LidarrDownloader in LidarrDownloaderStats
            lidarrDownloaderStats.SetLidarrDownloader(lidarrDownloader);

            if (settings.LidarrEnabled) // Only start if Lidarr is enabled
            {
                StartLidarrDownloaderAsync();
                InitializeDownloadTimer();
            }

            // Check if WebUI is enabled in your settings
            if (settings.WebUIEnabled)
            {
                //Debug.WriteLine("WebUI is enabled. Attempting to start WebUIServer...");
                string webUIUrl = string.IsNullOrWhiteSpace(settings.WebUIUrl) ? "http://+:5000/" : settings.WebUIUrl.Replace("localhost", "+");
                if (!webUIUrl.EndsWith("/"))
                {
                    webUIUrl += "/";
                }
                //Debug.WriteLine($"WebUI URL: {webUIUrl}");

                _webUIServer = new WebUIServer(lidarrDownloaderStats, lidarrDownloader, db, webUIUrl);
                Task.Run(async () =>
                {
                    try
                    {
                        //Debug.WriteLine("Calling WebUIServer.StartAsync()...");
                        await _webUIServer.StartAsync();
                        //Debug.WriteLine("WebUIServer.StartAsync() completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        //Debug.WriteLine($"Error starting WebUIServer: {ex.Message}");
                        //Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    }
                });
            }
            else
            {
                //Debug.WriteLine("WebUI is not enabled.");
            }

            InitializeSystemTrayIcon();
            // Attach the event handler for the Closing event
            this.Closing += MainWindow_Closing;

            // If db is used in the MainWindow context, initialize it as well
            db = dbManager;

            string jsonFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "d-fi.config.json");

            if (!File.Exists(jsonFilePath))
            {
                string defaultJson =
                            @"{
  ""ItemProcess"": 2,
  ""concurrency"": 2,
  ""saveLayout"": {
    ""track"": ""Music/{ALB_TITLE}/{SNG_TITLE}"",
    ""album"": ""{ALB_TITLE}/{SNG_TITLE}"",
    ""artist"": ""Music/{ALB_TITLE}/{SNG_TITLE}"",
    ""playlist"": ""Playlist/{TITLE}/{SNG_TITLE}"",
    ""qobuz-album"": ""{alb_artist}/{alb_title}/{title}"",
    ""qobuz-track"": null,
    ""qobuz-artist"": null
  },
  ""playlist"": {
    ""resolveFullPath"": false
  },
  ""trackNumber"": true,
  ""fallbackTrack"": true,
  ""fallbackQuality"": true,
  ""qobuzDownloadCover"": false,
  ""deezerDownloadCover"": false,
  ""coverSize"": {
    ""128"": 500,
    ""320"": 500,
    ""flac"": 1000
  },
  ""cookies"": {
    ""arl"": """"
  },
  ""qobuz"": {
    ""app_id"": 814460817,
    ""secrets"": ""10b251c286cfbf64d6b7105f253d9a2e,979549437fcc4a3faad4867b5cd25dcb"",
    ""token"": ""9WtCjUyV8CAsLSybfy_GFr_hyDW0OQEoCjsuUMkl8-vQV-18aK-KM6V_X_ihBGVFHQH9RhzHW_I7LWNzQgMVcw""
  },
  ""QobuzFolderPath"": "".\\Music"",
  ""DeezerFolderPath"": "".\\Music"",
  ""QobuzQuality"": ""FLAC - HiFi, 24-bit/192 kHz"",
  ""DeezerQuality"": ""flac"",
  ""LidarrEnabled"": false,
  ""LidarrUrl"": ""http://localhost:8686"",
  ""LidarrApiKey"": ""your_lidarr_api_key_here"",
  ""WebUIEnabled"": false,
  ""WebUIUrl"": ""http://localhost:5000""
}";

                File.WriteAllText(jsonFilePath, defaultJson);
            }

            // Now we know the file exists, so we can read and deserialize it
            string json = File.ReadAllText(jsonFilePath);
            settings = JsonConvert.DeserializeObject<Settings>(json); // Deserialize the JSON into the settings object
            downloadSemaphore = new SemaphoreSlim(settings.ItemProcess); // Initialize the semaphore

            string exePath = System.AppDomain.CurrentDomain.BaseDirectory + "d-fi.exe";
            if (!File.Exists(exePath))
            {
                MessageBoxResult result = MessageBox.Show("The d-fi.exe was not found. It is required for the functionality of the program. Do you want to download it?", "Download d-fi.exe", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DownloadAndUnzipDfi();
                }
            }
        }
        // FeaturedAlbum class definition
        public class FeaturedAlbum
        {
            public string Albums { get; set; }
            public string QobuzUrl { get; set; }
            public string QobuzPoster { get; set; }
            public string Qposterlabel { get; set; }
            public string Genrelabel { get; set; }
        }


        private void LoadSettings()
        {
            string jsonFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "d-fi.config.json");
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                settings = JsonConvert.DeserializeObject<Settings>(json);

                // Ensure new properties are initialized
                if (settings.LidarrEnabled == null)
                    settings.LidarrEnabled = false;
                if (string.IsNullOrEmpty(settings.LidarrUrl))
                    settings.LidarrUrl = "http://localhost:8686";
                if (string.IsNullOrEmpty(settings.LidarrApiKey))
                    settings.LidarrApiKey = "";
            }
            else
            {
                // Create default settings
                settings = new Settings();
                // ... (initialize other properties)
            }
        }
        private void InitializeDependencies(LidarrDownloaderStats stats)
        {
            try
            {
                lidarrDownloadSemaphore = new SemaphoreSlim(settings.ItemProcess, settings.ItemProcess);
                DatabaseManager db = new DatabaseManager();
                //Debug.WriteLine("Initializing LidarrDownloader with a valid semaphore.");
                lidarrDownloader = new LidarrDownloader(downloadManagerControl, db, Dispatcher, stats, lidarrDownloadSemaphore, settings, this);
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error initializing dependencies: {ex.Message}");
                // Handle the error (e.g., show a message to the user)
            }
        }

        private void InitializeDownloadTimer()
        {
            downloadTimer = new DispatcherTimer();
            downloadTimer.Interval = TimeSpan.FromSeconds(120);
            downloadTimer.Tick += DownloadTimer_Tick;
            downloadTimer.Start();
        }

        private async void StartLidarrDownloaderAsync()
        {
            try
            {
                await lidarrDownloader.RunDownloadProcess();
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error in StartLidarrDownloaderAsync: {ex.Message}");
            }
        }

        private bool IsRunningAsAdmin()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true,
                Verb = "runas" // This will trigger the UAC prompt for administrative privileges
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart as administrator: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void DownloadTimer_Tick(object sender, EventArgs e)
        {
            if (settings.LidarrEnabled)
            {
                try
                {
                    await lidarrDownloader.RunDownloadProcess();
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine($"Error in periodic check: {ex.Message}");
                }
            }
        }


        /// <summary>
        /// SYSTEM TRAY METHODS////
        /// </summary>

        private void InitializeSystemTrayIcon()
        {
            try
            {
                trayIconContextMenu = new System.Windows.Controls.ContextMenu();
                openMenuItem = new System.Windows.Controls.MenuItem { Header = "Open" };
                exitMenuItem = new System.Windows.Controls.MenuItem { Header = "Exit" };

                openMenuItem.Click += OpenMenuItem_Click;
                exitMenuItem.Click += ExitMenuItem_Click;

                trayIconContextMenu.Items.Add(openMenuItem);
                trayIconContextMenu.Items.Add(exitMenuItem);

                taskbarIcon = new TaskbarIcon();

                // Use embedded resource for icon
                Uri iconUri = new Uri("pack://application:,,,/BeatOn_Logo.ico");
                taskbarIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(iconUri).Stream);

                taskbarIcon.ToolTipText = "BeatOn";
                taskbarIcon.ContextMenu = trayIconContextMenu;
                taskbarIcon.TrayLeftMouseDown += TaskbarIcon_TrayLeftMouseDown;
                taskbarIcon.Visibility = System.Windows.Visibility.Hidden;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing system tray icon: {ex.Message}\n\nStack Trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate(); // This will bring the window to the foreground
            this.Focus();    // This will set focus to the window
            isMinimizedToTray = false;
            taskbarIcon.Visibility = System.Windows.Visibility.Hidden;
        }

        private bool LidarrFeatureIsEnabled
        {
            get
            {
                // Replace this with your actual logic to check if Lidarr feature is enabled
                return settings.LidarrEnabled;
            }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized && LidarrFeatureIsEnabled)
            {
                this.Hide();
                taskbarIcon.Visibility = System.Windows.Visibility.Visible;
                isMinimizedToTray = true;
            }
            base.OnStateChanged(e);
        }

        //END//
        //   //
        //   //

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Music_Data_Albums_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void LeftTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async Task SearchDeezer(string searchType)
        {
            Deezer_Artist_Listbox.Items.Clear();
            Deezer_Albums_Listbox.Items.Clear();
            Deezer_Songs_Listbox.Items.Clear();
            Deezer_Playlist_Listbox.Items.Clear();

            string searchRegex = Regex.Replace(Search_Box.Text, "[^a-zA-Z0-9\\s]", "").Replace("\\s+", "-");
            string baseUri = $"https://api.deezer.com/search/{searchType}/?q={searchRegex}&index=0&limit=50&output=json";

            using (HttpClient client = new HttpClient())
            {
                string json = await client.GetStringAsync(baseUri);
                dynamic data = JsonConvert.DeserializeObject(json);

                foreach (var item in data.data)
                {
                    SearchResult result = new SearchResult();

                    if (item == null) continue;  // Skip this iteration if item is null

                    switch (searchType)
                    {
                        case "album":
                            if (item.artist != null && item.artist.name != null && item.title != null)
                            {
                                result.Name = $"{item.artist.name} - {item.title.Substring(0, Math.Min(60, item.title.Length))}";
                                result.Title = item.title;
                                result.Genre = "ARTIST ALBUMS";
                                Deezer_Albums_Listbox.Items.Add(result.Name);
                            }
                            result.Picture = item.cover_xl != null ? item.cover_xl : null;
                            break;
                        case "artist":
                            if (item.name != null)
                            {
                                result.Name = item.name;
                                result.Title = item.name;
                                result.Genre = "ARTIST";
                                Deezer_Artist_Listbox.Items.Add(result.Name);
                            }
                            result.Picture = item.picture_xl != null ? item.picture_xl : null;
                            break;
                        case "track":
                            if (item.artist != null && item.artist.name != null && item.title != null && item.album != null && item.album.title != null)
                            {
                                result.Name = $"{item.artist.name} - {item.title} - {item.album.title}";
                                result.Title = $"{item.artist.name} - {item.title}";
                                result.Genre = "SONGS";
                                Deezer_Songs_Listbox.Items.Add(result.Name);
                            }
                            result.Picture = item.album != null && item.album.cover_xl != null ? item.album.cover_xl : null;
                            break;
                        case "playlist":
                            if (item.title != null)
                            {
                                result.Name = $"{item.title} - {item.type}";
                                result.Title = item.title;
                                result.Genre = "PLAYLIST";
                                Deezer_Playlist_Listbox.Items.Add(result.Name);
                            }
                            result.Picture = item.picture_xl != null ? item.picture_xl : null;
                            break;
                    }

                    result.Link = item.link != null ? item.link : null;
                    // TODO: Store result somewhere for later use
                }
            }

            Search_Box.Text = string.Empty;
        }

        private void Search_Box_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == "Enter Title\\Album\\Track Or Url...")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.Black; // Change text color to normal
            }
        }

        private void Search_Box_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Enter Title\\Album\\Track Or Url...";
                textBox.Foreground = Brushes.Gray; // Change text color to gray
            }
        }

        private int ParseProgress(string consoleOutput)
        {
            // Your parsing logic here, which depends on the format of the d-fi.exe console output.
            // For the sake of example, let's say d-fi.exe outputs progress as 'Progress: XX%'
            var match = Regex.Match(consoleOutput, @"Progress: (\d+)%");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            else
            {
                return -1;
            }
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteSearch();
        }

        private async void txt1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await ExecuteSearch();
            }
        }
        private async Task ExecuteSearch()
        {
            bool isQobuzSearch = false;
            string searchType = "";

            // Nested method: GetQobuzSearchApiPath
            string GetQobuzSearchApiPath(string searchType)
            {
                switch (searchType)
                {
                    case "artist":
                        return "artist/search?query=";
                    case "track":
                        return "catalog/search?query=";
                    case "album":
                        return "album/search?query=";
                    default:
                        throw new ArgumentException($"Invalid search type: {searchType}");
                }
            }


            try
            {
                try
                {
                    string json = File.ReadAllText("d-fi.config.json");
                    settings = JsonConvert.DeserializeObject<Settings>(json);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error reading settings file: {ex.Message}");
                    return;
                }

                if (MainTabControl.SelectedItem == Qobuz_TabItem)
                {
                    isQobuzSearch = true;

                    if (QobuzTabTabControl.SelectedItem == Qobuz_Artist_TabItem)
                    {
                        searchType = "artist";
                        Qobuz_Artist_Listbox.Items.Clear();
                        Qobuz_Artist_List.Clear();
                    }
                    else if (QobuzTabTabControl.SelectedItem == Qobuz_Songs_TabItem)
                    {
                        searchType = "track";
                        Qobuz_Songs_Listbox.Items.Clear();
                        Qobuz_Songs_List.Clear();
                    }
                    else if (QobuzTabTabControl.SelectedItem == Qobuz_Albums_TabItem)
                    {
                        searchType = "album";
                        Qobuz_Albums_Listbox.Items.Clear();
                        Qobuz_Albums_List.Clear();
                    }
                    //else if (QobuzTabTabControl.SelectedItem == Qobuz_Playlist_TabItem)
                    //{
                    //    searchType = "playlist";
                    //    Qobuz_Playlist_Listbox.Items.Clear();
                    //    Qobuz_Playlist_List.Clear();
                    //}
                }
                else if (DeezerTabTabControl.SelectedItem == Artist_TabItem)
                {
                    searchType = "artist";
                    Deezer_Artist_Listbox.Items.Clear();
                    artistList.Clear();
                }
                else if (DeezerTabTabControl.SelectedItem == Songs_TabItem)
                {
                    searchType = "track";
                    Deezer_Songs_Listbox.Items.Clear();
                    songsList.Clear();
                }
                else if (DeezerTabTabControl.SelectedItem == Albums_TabItem)
                {
                    searchType = "album";
                    Deezer_Albums_Listbox.Items.Clear();
                    albumsList.Clear();
                }
                else if (DeezerTabTabControl.SelectedItem == Playlist_TabItem)
                {
                    searchType = "playlist";
                    Deezer_Playlist_Listbox.Items.Clear();
                    playlistList.Clear();
                }

                string searchRegex = Regex.Replace(Search_Box.Text, "[^a-zA-Z0-9\\s]", "").Replace("\\s+", "-");

                if (isQobuzSearch)
                {
                    // Handle Qobuz search
                    string baseUri = $"https://www.qobuz.com/api.json/0.2/{GetQobuzSearchApiPath(searchType)}{searchRegex}&limit=50";

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                        var queryParameters = new Dictionary<string, string>
                        {
                         { "app_secret", settings.qobuz.secrets.ToString() },
                         { "app_id", settings.qobuz.app_id.ToString() }
                        };

                        var content = new FormUrlEncodedContent(queryParameters);

                        var response = await client.PostAsync(baseUri, content);
                        response.EnsureSuccessStatusCode();

                        string json = await response.Content.ReadAsStringAsync();


                        JObject data = JObject.Parse(json);

                        // Check if data is null or if specific keys based on searchType are missing
                        if (data == null || (searchType == "artist" && data["artists"] == null) || (searchType != "artist" && data["albums"] == null))
                        {
                            MessageBox.Show("No data received from the Qobuz API. Please check your search query or API endpoint.");
                            return;
                        }

                        if (searchType == "track")
                        {
                            JArray tracksArray = data["tracks"]["items"] as JArray;

                            foreach (JToken track in tracksArray)
                            {
                                SearchResult result = new SearchResult();

                                if (track == null) continue;  // Skip this iteration if track is null
                                string title = track["title"].ToString();
                                result.Albums = $"{track["performer"]["name"]} - {title.Substring(0, Math.Min(60, title.Length))} - {track["maximum_bit_depth"]}bit/{track["maximum_sampling_rate"]}kHz";
                                result.QobuzUrl = $"http://open.qobuz.com/track/{track["id"]}";
                                result.QobuzPoster = track["album"]["image"]["large"] != null ? track["album"]["image"]["large"].ToString() : null;
                                result.Qposterlabel = $"{track["performer"]["name"]} - {track["title"]} ({track["album"]["release_date_original"]})";
                                result.Genrelabel = "SONGS";
                                Qobuz_Songs_Listbox.Items.Add(result);
                                Qobuz_Songs_List.Add(result);
                            }
                        }
                        else if (searchType == "artist")
                        {
                            JArray artistsArray = data["artists"]["items"] as JArray;

                            foreach (JToken artist in artistsArray)
                            {
                                SearchResult result = new SearchResult();

                                if (artist == null) continue;  // Skip this iteration if artist is null
                                string name = artist["name"].ToString();
                                string artistId = artist["id"].ToString();                              
                                // Safely access the 'large' image URL
                                string artistPicture = null;
                                JToken imageToken = artist["image"];
                                if (imageToken != null && imageToken.Type != JTokenType.Null)
                                {
                                    JToken largeImageToken = imageToken["large"];
                                    if (largeImageToken != null && largeImageToken.Type != JTokenType.Null)
                                    {
                                        artistPicture = largeImageToken.ToString();
                                    }
                                }

                                // If 'artistPicture' is still null, assign a default image URL or keep it as null
                                if (string.IsNullOrEmpty(artistPicture))
                                {
                                    artistPicture = "path/to/default/image.jpg"; // Replace with your default image path or keep it null
                                }

                                result.Albums = name; // Assuming 'Albums' is used to store artist name
                                result.QobuzUrl = $"http://open.qobuz.com/artist/{artistId}";
                                result.QobuzPoster = artistPicture;
                                result.Qposterlabel = name; // Or any other format you prefer
                                result.Genrelabel = "ARTIST"; // Or any suitable label
                                result.ArtistId = artistId;
                                Qobuz_Artist_Listbox.Items.Add(result);
                                Qobuz_Artist_List.Add(result);
                            }
                        }
                        else if (searchType == "album")
                        {
                            JArray albumsArray = data["albums"]["items"] as JArray;

                            foreach (JToken album in albumsArray)
                            {
                                SearchResult result = new SearchResult();

                                if (album == null) continue;  // Skip this iteration if album is null
                                string title = album["title"].ToString();
                                result.Albums = $"{album["artist"]["name"]} - {title.Substring(0, Math.Min(60, title.Length))} - {album["maximum_bit_depth"]}bit/{album["maximum_sampling_rate"]}kHz";
                                result.albumId = $"{album["id"]}";
                                result.QobuzUrl = $"http://open.qobuz.com/album/{album["id"]}";
                                result.QobuzPoster = album["image"]["large"] != null ? album["image"]["large"].ToString() : null;
                                result.Qposterlabel = $"{album["artist"]["name"]} - {album["title"]} ({album["release_date_original"]})";
                                result.Genrelabel = "ARTIST ALBUMS";
                                Qobuz_Albums_Listbox.Items.Add(result);
                                Qobuz_Albums_List.Add(result);
                            }
                        }


                    }

                }
                else
                {
                    // Handle Deezer search

                    using (HttpClient client = new HttpClient())
                    {
                        for (int i = 0; i < 2; i++)  // Iterate for 2 pages (0 and 1)
                        {
                            string baseUri = $"https://api.deezer.com/search/{searchType}/?q={searchRegex}&index={i * 100}&limit=100&output=json";

                            string json = await client.GetStringAsync(baseUri);
                            dynamic data = JsonConvert.DeserializeObject(json);

                            if (data == null || data.data == null)
                            {
                                MessageBox.Show("No data received from the Deezer API. Please check your search query or API endpoint.");
                                return;
                            }

                            foreach (var item in data.data)
                            {
                                SearchResult result = new SearchResult();

                                if (searchType == "artist")
                                {
                                    result.artistId = item.id;
                                    result.Albums = item.name;
                                    result.QobuzUrl = item.link;
                                    result.QobuzPoster = item.picture_xl;
                                    result.Qposterlabel = item.name;
                                    result.Genrelabel = "ARTIST";
                                    Deezer_Artist_Listbox.Items.Add(result);
                                    artistList.Add(result);
                                }
                                else if (searchType == "track")
                                {
                                    result.Albums = $"{item.artist.name} - {item.album.title}: {item.title}";
                                    if (item.explicit_lyrics == true)  // Check if the album is marked as explicit
                                    {
                                        result.Albums += " (Explicit)";
                                    }
                                    result.QobuzUrl = item.link;
                                    result.QobuzPoster = item.album.cover_xl;
                                    int totalSeconds = item.duration;
                                    int minutes = totalSeconds / 60;
                                    int seconds = totalSeconds % 60;
                                    string durationFormatted = $"{minutes.ToString("D2")}:{seconds.ToString("D2")}";  // Formats as "MM:SS"
                                    result.Qposterlabel = $"{item.artist.name} - {item.album.title}: {item.title} (Duration: {durationFormatted})";
                                    result.Genrelabel = "SONGS";
                                    result.SongsPreview = item.preview;
                                    Deezer_Songs_Listbox.Items.Add(result);
                                    songsList.Add(result);
                                }
                                else if (searchType == "album")
                                {
                                    result.Albums = $"{item.artist.name} - {item.title}";
                                    if (item.explicit_lyrics == true)  // Check if the album is marked as explicit
                                    {
                                        result.Albums += " (Explicit)";
                                    }
                                    result.albumId = $"{item.id}";
                                    result.QobuzUrl = item.link;
                                    result.QobuzPoster = item.cover_xl;
                                    result.Qposterlabel = $"{item.artist.name} - {item.title}";
                                    result.Genrelabel = "ARTIST ALBUMS";
                                    Deezer_Albums_Listbox.Items.Add(result);
                                    albumsList.Add(result);
                                }
                                else if (searchType == "playlist")
                                {
                                    result.Albums = $"{item.title} - {item.type}";
                                    result.QobuzUrl = item.link;
                                    result.QobuzPoster = item.picture_xl;
                                    result.Qposterlabel = item.title;
                                    result.Genrelabel = "PLAYLIST";
                                    Deezer_Playlist_Listbox.Items.Add(result);
                                    playlistList.Add(result);
                                }
                            }
                        }
                    }
                }

                Search_Box.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void Qobuz_Artist_Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedItem = Qobuz_Artist_Listbox.SelectedItem as SearchResult;

            if (selectedItem != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();

                // Check if the URI is well-formed and not null or empty
                if (!string.IsNullOrEmpty(selectedItem.QobuzPoster) && Uri.IsWellFormedUriString(selectedItem.QobuzPoster, UriKind.Absolute))
                {
                    bitmap.UriSource = new Uri(selectedItem.QobuzPoster);
                }
                else
                {
                    // Fallback URL if the original URL is not well-formed
                    bitmap.UriSource = new Uri("https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png");
                }

                bitmap.EndInit();
                AccessText accessText = (AccessText)Poster_Label.Content;
                accessText.Text = selectedItem.Qposterlabel;

                Albums_Covers.Source = bitmap;  // Image object with name "Albums_Covers"
            }
        }

        private void Qobuz_Songs_Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedItem = Qobuz_Songs_Listbox.SelectedItem as SearchResult;

            if (selectedItem != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = !string.IsNullOrEmpty(selectedItem.QobuzPoster) ? new Uri(selectedItem.QobuzPoster) : new Uri("https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png");
                bitmap.EndInit();

                Albums_Covers.Source = bitmap;  // Image object with name "Albums_Covers"
                AccessText accessText = (AccessText)Poster_Label.Content;
                accessText.Text = selectedItem.Qposterlabel;
            }
        }

        private void Qobuz_Albums_Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedItem = Qobuz_Albums_Listbox.SelectedItem as SearchResult;

            if (selectedItem != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = !string.IsNullOrEmpty(selectedItem.QobuzPoster) ? new Uri(selectedItem.QobuzPoster) : new Uri("https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png");
                bitmap.EndInit();

                Albums_Covers.Source = bitmap;  // Image object with name "Albums_Covers"
                AccessText accessText = (AccessText)Poster_Label.Content;
                accessText.Text = selectedItem.Qposterlabel;
            }
        }

        private void Deezer_Artist_Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedItem = Deezer_Artist_Listbox.SelectedItem as SearchResult;

            if (selectedItem != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = !string.IsNullOrEmpty(selectedItem.QobuzPoster) ? new Uri(selectedItem.QobuzPoster) : new Uri("https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png");
                bitmap.EndInit();

                Albums_Covers.Source = bitmap;  // Image object with name "ArtistImage"
                AccessText accessText = (AccessText)Poster_Label.Content;
                accessText.Text = selectedItem.Qposterlabel;
            }
        }

        private void Deezer_Songs_Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedItem = Deezer_Songs_Listbox.SelectedItem as SearchResult;

            if (selectedItem != null)
            {

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = !string.IsNullOrEmpty(selectedItem.QobuzPoster) ? new Uri(selectedItem.QobuzPoster) : new Uri("https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png");
                bitmap.EndInit();

                Albums_Covers.Source = bitmap;  // Image object with name "Albums_Covers"
                AccessText accessText = (AccessText)Poster_Label.Content;
                accessText.Text = selectedItem.Qposterlabel;             //Poster_Label.Content = song.Qposterlabel;  // Assuming you have Label object with name "Poster_Label"                                                                                  //AlbumLabel.Content = song.Genrelabel;  // Assuming you have Label object with name "AlbumLabel"              

            }
        }

        private void Deezer_Albums_Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedItem = Deezer_Albums_Listbox.SelectedItem as SearchResult;


            if (selectedItem != null)
            {

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = !string.IsNullOrEmpty(selectedItem.QobuzPoster) ? new Uri(selectedItem.QobuzPoster) : new Uri("https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png");
                bitmap.EndInit();

                Albums_Covers.Source = bitmap;  // Image object with name "ArtistImage"
                AccessText accessText = (AccessText)Poster_Label.Content;
                accessText.Text = selectedItem.Qposterlabel;                                //Poster_Label.Content = album.Qposterlabel;  // Assuming you have Label object with name "Poster_Label"
                                                                                            //AlbumLabel.Content = album.Genrelabel;  // Assuming you have Label object with name "AlbumLabel"
            }
        }

        private void Deezer_Playlist_Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchResult selectedItem = Deezer_Playlist_Listbox.SelectedItem as SearchResult;

            if (selectedItem != null)
            {

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = !string.IsNullOrEmpty(selectedItem.QobuzPoster) ? new Uri(selectedItem.QobuzPoster) : new Uri("https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png");
                bitmap.EndInit();

                Albums_Covers.Source = bitmap;  // Image object with name "PlaylistImage"
                                                //Poster_Label.Content = album.Qposterlabel;  // Assuming you have Label object with name "Poster_Label"
                AccessText accessText = (AccessText)Poster_Label.Content;
                accessText.Text = selectedItem.Qposterlabel;                              //PlaylistLabel.Content = album.Genrelabel;  // Assuming you have Label object with name "PlaylistLabel"

            }
        }

        private async void ArtistTopTracks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchResult selectedArtist = Deezer_Artist_Listbox.SelectedItem as SearchResult;
                if (selectedArtist != null)
                {
                    string artistId = selectedArtist.artistId;
                    string apiUri = $"https://api.deezer.com/artist/{artistId}/top?&limit=25";

                    using (HttpClient client = new HttpClient())
                    {
                        string json = await client.GetStringAsync(apiUri);
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data != null && data.data != null)
                        {
                            Deezer_Songs_Listbox.Items.Clear();// Clear existing songs if any

                            foreach (var item in data.data)
                            {
                                SearchResult trackResult = new SearchResult
                                {
                                    Albums = $"{item.artist.name} - {item.album.title}: {item.title}",
                                    QobuzUrl = item.link,
                                    QobuzPoster = item.album.cover_xl,
                                    Qposterlabel = $"{item.artist.name} - {item.album.title}: {item.title}",
                                    Genrelabel = "SONGS",
                                    SongsPreview = $"{item["preview"]}"
                                // Add other necessary properties
                            };

                                Deezer_Songs_Listbox.Items.Add(trackResult); // Add to your song list
                            }

                            MainTabControl.SelectedItem = DeezerTabTabControl; // Switch to Deezer tab
                            DeezerTabTabControl.SelectedItem = Songs_TabItem; // Switch to songs tab
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private async void ArtistRadio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchResult ArtistRadio = Deezer_Artist_Listbox.SelectedItem as SearchResult;
                if (ArtistRadio != null)
                {
                    string artistId = ArtistRadio.artistId;
                    string apiUri = $"https://api.deezer.com/artist/{artistId}/radio?&limit=50";

                    using (HttpClient client = new HttpClient())
                    {
                        string json = await client.GetStringAsync(apiUri);
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data != null && data.data != null)
                        {
                            Deezer_Songs_Listbox.Items.Clear();// Clear existing songs if any

                            foreach (var item in data.data)
                            {
                                SearchResult trackResult = new SearchResult
                                {
                                    Albums = $"{item.artist.name} - {item.album.title}: {item.title}",
                                    QobuzUrl = item.link,
                                    QobuzPoster = item.album.cover_xl,
                                    Qposterlabel = $"{item.artist.name} - {item.album.title}: {item.title}",
                                    Genrelabel = "SONGS",
                                    SongsPreview = $"{item["preview"]}"
                                    // Add other necessary properties
                                };

                                Deezer_Songs_Listbox.Items.Add(trackResult); // Add to your song list
                            }

                            MainTabControl.SelectedItem = DeezerTabTabControl; // Switch to Deezer tab
                            DeezerTabTabControl.SelectedItem = Songs_TabItem; // Switch to songs tab
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void Deezer_Artist_Listbox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var listBox = sender as ListBox;
            var selectedItem = listBox.SelectedItem as SearchResult;
            if (selectedItem == null)
            {
                e.Handled = true;
                return;
            }

            var contextMenu = new ContextMenu
            {
                Style = (Style)FindResource("CustomContextMenuStyle")
            };

            var downloadMenuItem = CreateMenuItem("Download", ContextMenuDownload_Click, selectedItem);
            contextMenu.Items.Add(downloadMenuItem);

            if (listBox.Name == "Deezer_Artist_Listbox")
            {
                var topTracksMenuItem = CreateMenuItem("Top Tracks", ArtistTopTracks_Click, selectedItem);
                contextMenu.Items.Add(topTracksMenuItem);

                var ArtistRadioItem = CreateMenuItem("Artist Radio", ArtistRadio_Click, selectedItem);
                contextMenu.Items.Add(ArtistRadioItem);

                var similarArtistsMenuItem = CreateMenuItem("Similar Artists", SimilarArtists_Click, selectedItem);
                contextMenu.Items.Add(similarArtistsMenuItem);
            }

            var urlMenuItem = CreateMenuItem("Url", ContextMenuLink_Click, selectedItem);
            contextMenu.Items.Add(urlMenuItem);

            listBox.ContextMenu = contextMenu;
        }

        private async void SimilarArtists_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchResult selectedArtist = Deezer_Artist_Listbox.SelectedItem as SearchResult;
                if (selectedArtist != null)
                {
                    string artistId = selectedArtist.artistId;
                    string apiUri = $"https://api.deezer.com/artist/{artistId}/related?&limit=25";

                    using (HttpClient client = new HttpClient())
                    {
                        string json = await client.GetStringAsync(apiUri);
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data != null && data.data != null)
                        {
                            Deezer_Artist_Listbox.Items.Clear(); // Clear existing artists if any

                            foreach (var artist in data.data)
                            {
                                SearchResult artistResult = new SearchResult
                                {
                                    artistId = artist.id,
                                    Albums = artist.name,
                                    QobuzUrl = artist.link,
                                    QobuzPoster = artist.picture_xl,
                                    Qposterlabel = artist.name,
                                    Genrelabel = "ARTISTS"
                                };

                                Deezer_Artist_Listbox.Items.Add(artistResult); // Add similar artist to the list
                            }

                            MainTabControl.SelectedItem = DeezerTabTabControl; // Switch to Deezer tab
                            DeezerTabTabControl.SelectedItem = Artist_TabItem; // Switch to artist tab
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private MenuItem CreateMenuItem(string header, RoutedEventHandler clickEventHandler, SearchResult dataContext)
        {
            var menuItem = new MenuItem
            {
                Header = header,
                Width = 100,
                FontWeight = FontWeights.SemiBold,
                DataContext = dataContext, // Set DataContext to the selected SearchResult
                Style = (Style)FindResource("CustomContextMenuItemStyle")
            };
            menuItem.Click += clickEventHandler;
            return menuItem;
        }


        private Dictionary<DownloadItem, Process> downloadProcesses = new Dictionary<DownloadItem, Process>();

        private Process process;

        private OutputWindow outputWindow; // Declare the outputWindow variable at the class level

        public class FeaturedSection
        {
            public string SectionName { get; set; }
            public List<FeaturedAlbum> Albums { get; set; }
        }

        public class AlbumImage
        {
            public string Small { get; set; }
        }

        public class Album
        {
            public string Title { get; set; }
            public dynamic Artist { get; set; }
            public string Id { get; set; }
            public AlbumImage Image { get; set; }
        }

        private List<Album> ConvertToAlbumList(dynamic dynamicAlbums)
        {
            List<Album> albums = new List<Album>();
            foreach (var album in dynamicAlbums)
            {
                albums.Add(new Album
                {
                    Title = album.title,
                    Artist = album.artist,
                    Id = album.id,
                    Image = new AlbumImage { Small = album.image.small }
                });
            }
            return albums;
        }

        private async void ContainerSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if settings have been loaded
            if (settings == null)
            {
                return; // If settings are not loaded yet, exit the method
            }
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            string selectedContainer = selectedItem.Content.ToString();

            // Clear existing items
            //Qobuz_Featured_SectionsControl.ItemsSource = null;

            switch (selectedContainer)
            {
                case "None":
                    Qobuz_Featured_SectionsControl.ItemsSource = null;
                    break;
                case "New Releases":
                    await LoadContainer("container-album-new-releases-full", "New Releases");
                    break;
                case "Recent Releases":
                    await LoadContainer("container-album-recent-releases", "Recent Releases");
                    break;
                case "Press Awards":
                    await LoadContainer("container-album-press-awards", "Press Awards");
                    break;
                case "Album Charts":
                    await LoadContainer("container-album-charts", "Album Charts");
                    break;
                case "Album Of The Week":
                    await LoadContainer("container-album-of-the-week", "Album Of The Week");
                    break;
                case "Re Release Of The Week":
                    await LoadContainer("container-re-release-of-the-week", "Re Release Of The Week");
                    break;
            }
        }

        private async Task LoadContainer(string containerName, string sectionName)
        {

            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading settings file: {ex.Message}");
                return;
            }

            var clientHandler = new HttpClientHandler
            {
                UseCookies = false,
            };
            var client = new HttpClient(clientHandler);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://www.qobuz.com/api.json/0.2/featured/albums"),
                Headers =
        {
            { "cookie", "qobuz-session-aws=8dfa386b083a92139bf2de731e9d9d6c%3A0d260e5db2b16f36be931fc6dedb8dd65aa17a35" },
        },
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "app_secret", settings.qobuz.secrets.ToString() },
            { "app_id", settings.qobuz.app_id.ToString() },
        }),
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseObject = JsonConvert.DeserializeObject(responseBody);
                dynamic containerItems = responseObject.containers[containerName].albums.items;

                List<Album> albums = ConvertToAlbumList(containerItems);
                string qobuzMainLink = "http://open.qobuz.com/album/";

                List<FeaturedSection> featuredSections = new List<FeaturedSection>
        {
            new FeaturedSection
            {
                SectionName = sectionName,
                Albums = albums.Select(album => CreateFeaturedAlbum(album, sectionName, qobuzMainLink)).ToList()
            }
        };

                Dispatcher.Invoke(() =>
                {
                    Qobuz_Featured_SectionsControl.ItemsSource = featuredSections;
                });
            }
        }

        private FeaturedAlbum CreateFeaturedAlbum(Album album, string genreLabel, string qobuzMainLink)
        {
            string albumTitle = album.Title;
            string artistName = album.Artist.name;
            string qobuzId = album.Id;
            string qobuzPoster = album.Image.Small;

            return new FeaturedAlbum
            {
                Albums = $"{artistName.Substring(0, Math.Min(60, artistName.Length))} - {albumTitle.Substring(0, Math.Min(60, albumTitle.Length))}",
                QobuzUrl = $"{qobuzMainLink}{qobuzId}",
                QobuzPoster = qobuzPoster,
                Qposterlabel = $"{artistName} - {albumTitle}",
                Genrelabel = genreLabel
            };
        }

        private async Task DownloadQobuzAlbum(FeaturedAlbum selectedAlbum)
        {
            DownloadItem downloadItem = new DownloadItem
            {
                Name = selectedAlbum.Qposterlabel,
                Progress = 0,
                StartTime = DateTime.Now,
                Url = selectedAlbum.QobuzUrl,
                Status = "Waiting", // Set the initial status to "waiting"
                SourceType = "Qobuz" // Make sure this is set for Qobuz items
            };

            if (downloadManagerControl != null)
            {
                downloadManagerControl.AddDownloadItem(downloadItem);
            }

            await downloadSemaphore.WaitAsync(); // Wait for available download slot

            string qobuzQualityValue = MapQualityToValue(settings.QobuzQuality);

            // Update the status to "In progress" once the download slot is available
            downloadItem.Status = "In progress";

            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading settings file: {ex.Message}");
                downloadSemaphore.Release(); // Release the slot if there's an error
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = @".\d-fi.exe",
                Arguments = $@"/c -u '{selectedAlbum.QobuzUrl}' -q ""{qobuzQualityValue}"" -d -b -o ""{settings.QobuzFolderPath}/{settings.saveLayout.qobuzAlbum}"""
            };

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;

            Process process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            downloadItem.DownloadProcess = process;
            int totalCount = 0;
            activeDownloads++;
            UpdateDownloadBadge();
            process.OutputDataReceived += (s, output) =>
            {
                if (!string.IsNullOrEmpty(output.Data))
                {
                    string outputMessage = output.Data;

                    if (outputMessage.StartsWith("✔ Path:"))
                    {
                        // Extract the path for the individual track
                        string trackPath = outputMessage.Substring("✔ Path:".Length).Trim();
                        // Store the first track path if none has been stored yet
                        if (string.IsNullOrEmpty(downloadItem.DownloadPath))
                        {
                            downloadItem.DownloadPath = trackPath;
                        }
                    }
                    else if (outputMessage.StartsWith("ℹ info Saved in:"))
                    {
                        // Set flag to capture the album path on the next line
                        captureNextLineAsAlbumPath = true;
                    }
                    else if (captureNextLineAsAlbumPath)
                    {
                        // Capture the album path
                        string albumPath = outputMessage.Trim();
                        // Store the album path
                        downloadItem.AlbumDownloadPath = albumPath;
                        captureNextLineAsAlbumPath = false;
                    }

                    if (outputMessage.Contains("ℹ info") ||
                        outputMessage.Contains("⚠ warn") ||
                        outputMessage.Contains("● pending") ||
                        outputMessage.Contains("✔ success") ||
                        outputMessage.Contains("✔ Path:") ||
                        outputMessage.Contains("✖ error"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            string cleanedOutputMessage = RemoveAnsiEscapeSequences(outputMessage);
                            downloadItem.RawOutput += cleanedOutputMessage + Environment.NewLine;

                            // Check and append success or error messages to the Log
                            if (cleanedOutputMessage.Contains("✔ success") || cleanedOutputMessage.Contains("✖ error"))
                            {
                                downloadItem.Log += cleanedOutputMessage + Environment.NewLine;
                            }

                            // Check and append path information to the Log
                            if (outputMessage.StartsWith("✔ Path:"))
                            {
                                string pathMessage = cleanedOutputMessage.Substring("✔ Path:".Length).Trim();
                                downloadItem.Log += "Path: " + pathMessage + Environment.NewLine;
                            }

                            if (downloadManagerWindow != null)
                            {
                                Task.Run(() => downloadManagerWindow.UpdateOutput(cleanedOutputMessage));
                            }
                        });
                    }
                }
            };

            process.Exited += (s, exited) =>
            {
                downloadItem.EndTime = DateTime.Now;

                if (process.ExitCode == 0)
                {
                    downloadItem.Status = "Completed";
                }
                else
                {
                    downloadItem.Status = "Failed";
                    downloadItem.ErrorMessage = downloadItem.Output;
                }

                db.UpdateDownloadItem(downloadItem);

                if (downloadManagerWindow != null)
                {
                    downloadManagerWindow.Dispatcher.Invoke(() => downloadManagerWindow.UpdateDownloadItemProgress(downloadItem, totalCount, totalCount));
                }

                process.Dispose();
                downloadSemaphore.Release();
                activeDownloads--;
                Dispatcher.Invoke(() => UpdateDownloadBadge());
            };

            await Task.Run(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
            });
        }

        private void UpdateDownloadBadge()
        {
            Dispatcher.Invoke(() =>
            {
                DownloadBadgeCount.Text = activeDownloads.ToString();
                DownloadBadge.Visibility = activeDownloads > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private string RemoveAnsiEscapeSequences(string input)
        {
            string pattern = @"\x1B\[[^@-~]*[@-~]";
            string cleanedInput = Regex.Replace(input, pattern, string.Empty);
            return cleanedInput;
        }

        private async void GenreSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if settings have been loaded
            if (settings == null)
            {
                return; // If settings are not loaded yet, exit the method
            }
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
            string selectedGenre = selectedItem.Content.ToString();

            switch (selectedGenre)
            {
                case "None":
                    Qobuz_Genre_SectionsControl.ItemsSource = null;
                    break;
                case "Rap":
                    await LoadGenre("genre_ids=133%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "Rap");
                    break;
                case "Pop":
                    await LoadGenre("genre_ids=112%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "Pop");
                    break;
                case "Latin":
                    await LoadGenre("genre_ids=149%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "Latin");
                    break;
                case "Electro":
                    await LoadGenre("genre_ids=64%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "Electro");
                    break;
                case "Classical":
                    await LoadGenre("genre_ids=10%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "Classical");
                    break;
                case "RNB":
                    await LoadGenre("genre_ids=127%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "RNB");
                    break;
                case "World":
                    await LoadGenre("genre_ids=94%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "World");
                    break;
                case "Comedy":
                    await LoadGenre("genre_ids=59%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&", "Comedy");
                    break;
            }
        }

        private async Task LoadGenre(string selectedGenre, string sectionName)
        {
            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading settings file: {ex.Message}");
                return;
            }

            var clientHandler = new HttpClientHandler
            {
                UseCookies = false,
            };
            var client = new HttpClient(clientHandler);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://www.qobuz.com/api.json/0.2/featured/index?" + selectedGenre),
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "app_secret", settings.qobuz.secrets.ToString() },
        { "app_id", settings.qobuz.app_id.ToString() },
    }),
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic responseObject = JsonConvert.DeserializeObject(responseBody);
                dynamic containerItems = responseObject.containers["container-album-new-releases"].albums.items;

                List<Album> albums = ConvertToAlbumList(containerItems);
                string qobuzMainLink = "http://open.qobuz.com/album/";

                List<FeaturedSection> featuredSections = new List<FeaturedSection>
    {
        new FeaturedSection
        {
            SectionName = sectionName,
            Albums = albums.Select(album => CreateFeaturedAlbum(album, sectionName, qobuzMainLink)).ToList()
        }
    };

                Dispatcher.Invoke(() =>
                {
                    Qobuz_Genre_SectionsControl.ItemsSource = featuredSections; // Update the control name if different
                });
            }

        }

        private string MapQualityToValue(string qualityText)
        {
            switch (qualityText)
            {
                case "MP3  - 320 kbps":
                    return "5";
                case "FLAC - CD, 16-bit/44.1 kHz":
                    return "6";
                case "FLAC - HiFi, 24-bit/96 kHz":
                    return "7";
                case "FLAC - HiFi, 24-bit/192 kHz":
                    return "27";
                default:
                    return "5"; // Default to MP3 320kbps if no match found
            }
        }

        bool captureNextLineAsAlbumPath = false;
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var dbFileName = "downloads.db";
            var dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);

            if (!File.Exists(dbFilePath))
            {
                try
                {
                    //Debug.WriteLine("Database file not found during download operation. Attempting to extract from resources.");
                    ExtractEmbeddedResource("WpfApp1.downloads.db", dbFilePath);
                    //Debug.WriteLine("Successfully extracted database file.");

                    // Introduce a delay to ensure the file is completely written
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to recreate the database. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading settings file: {ex.Message}");
                return;
            }
            
            Button button = sender as Button;
            FeaturedAlbum selectedAlbum = button.Tag as FeaturedAlbum;
            SearchResult searchResult = null;
            string urlToDownload = "";

            // Determine the URL based on the checkbox state
            if (UrlCheckBox.IsChecked == true)
            {
                urlToDownload = Search_Box.Text; // Use the text from the Search_Box
            }
            else
            {

                if (MainTabControl.SelectedIndex == 0) // Assuming DeezerTabTabControl is the first tab
                { 
                    // Check the selected tab and set the search result accordingly
                    if (DeezerTabTabControl.SelectedItem == Artist_TabItem)
                    {
                        if (Deezer_Artist_Listbox.SelectedItem != null)
                        {
                            searchResult = (SearchResult)Deezer_Artist_Listbox.SelectedItem;
                        }
                    }
                    else if (DeezerTabTabControl.SelectedItem == Songs_TabItem)
                    {
                        if (Deezer_Songs_Listbox.SelectedItem != null)
                        {
                            searchResult = (SearchResult)Deezer_Songs_Listbox.SelectedItem;
                        }
                    }
                    else if (DeezerTabTabControl.SelectedItem == Albums_TabItem)
                    {
                        // Handle multiple selections in the Deezer Albums tab
                        var selectedAlbums = Deezer_Albums_Listbox.SelectedItems;
                        if (selectedAlbums.Count > 0)
                        {
                            // Creating a copy of the list to avoid collection modification issues
                            var albumsToDownload = selectedAlbums.Cast<SearchResult>().ToList();
                            foreach (SearchResult album in albumsToDownload)
                            {
                                // For each selected album, start the download
                                await StartDownloadFromSearchResult(album);
                            }
                        }
                    }
                    else if (DeezerTabTabControl.SelectedItem == Playlist_TabItem)
                    {
                        if (Deezer_Playlist_Listbox.SelectedItem != null)
                        {
                            searchResult = (SearchResult)Deezer_Playlist_Listbox.SelectedItem;
                        }
                    }
                }
                else if (MainTabControl.SelectedIndex == 1) // Assuming QobuzTabTabControl is the second tab
                {
                    if (MainTabControl.SelectedIndex == 1 && QobuzTabTabControl.SelectedItem == Qobuz_Featured_TabItem)
                    {
                        // Featured tab is selected in the Qobuz section, call DownloadQobuzAlbum
                        await DownloadQobuzAlbum(selectedAlbum);
                    }
                    else if (MainTabControl.SelectedIndex == 1 && QobuzTabTabControl.SelectedItem == Qobuz_Genre_TabItem)
                    {
                        // Featured tab is selected in the Qobuz section, call DownloadQobuzAlbum
                        await DownloadQobuzAlbum(selectedAlbum);
                    }
                    else
                    {
                        // Check the selected tab and set the search result accordingly
                        if (QobuzTabTabControl.SelectedItem == Qobuz_Artist_TabItem)
                        {
                            if (Qobuz_Artist_Listbox.SelectedItem != null)
                            {
                                searchResult = (SearchResult)Qobuz_Artist_Listbox.SelectedItem;
                            }
                        }
                        else if (QobuzTabTabControl.SelectedItem == Qobuz_Songs_TabItem)
                        {
                            if (Qobuz_Songs_Listbox.SelectedItem != null)
                            {
                                searchResult = (SearchResult)Qobuz_Songs_Listbox.SelectedItem;
                            }
                        }
                        else if (QobuzTabTabControl.SelectedItem == Qobuz_Albums_TabItem)
                        {
                            // Handle multiple selections in the Qobuz Albums tab
                            var selectedAlbums = Qobuz_Albums_Listbox.SelectedItems;

                            if (selectedAlbums.Count > 0)
                            {
                                // Creating a copy of the list to avoid collection modification issues
                                var albumsToDownload = selectedAlbums.Cast<SearchResult>().ToList();

                                foreach (SearchResult album in albumsToDownload)
                                {
                                    // For each selected album, start the download
                                    await StartDownloadFromSearchResult(album);
                                }
                            }
                        }
                    }
                }
                
                if (searchResult != null)
                {
                    urlToDownload = searchResult.QobuzUrl;
                }

            }
            if (!string.IsNullOrEmpty(urlToDownload))
            {
                DownloadItem downloadItem = new DownloadItem
                {
                    Name = UrlCheckBox.IsChecked == true ? urlToDownload : (searchResult != null ? searchResult.Albums : "Unknown"),
                    Progress = 0,
                    StartTime = DateTime.Now,
                    Url = urlToDownload,
                    Status = "Waiting", // Set the initial status to "Waiting"
                    SourceType = MainTabControl.SelectedIndex == 0 ? "Deezer" : "Qobuz"

                };

                // Use the DbManager from downloadManagerControl
                if (downloadManagerControl != null && downloadManagerControl.DbManager != null)
                {
                    downloadManagerControl.DbManager.SaveDownloadItem(downloadItem);
                }
                else
                {
                    MessageBox.Show("Database Manager is not initialized.");
                    return;
                }

                // Check if downloadManagerControl is not null
                if (downloadManagerControl != null)
                {
                    downloadManagerControl.AddDownloadItem(downloadItem); // Add the download item to the DownloadManagerControl
                }

                string qobuzQualityValue = MapQualityToValue(settings.QobuzQuality);

                ProcessStartInfo startInfo = new ProcessStartInfo();

                if (MainTabControl.SelectedIndex == 0) // Deezer tab
                {
                    startInfo.FileName = @".\d-fi.exe";
                    startInfo.Arguments = $@"/c -u '{urlToDownload}' -q ""{settings.DeezerQuality}"" -o ""{settings.DeezerFolderPath}/{settings.saveLayout.album}"" -d";
                }
                else if (MainTabControl.SelectedIndex == 1) // Qobuz tab
                {
                    startInfo.FileName = @".\d-fi.exe";
                    startInfo.Arguments = $@"/c -u '{urlToDownload}' -q ""{qobuzQualityValue}"" -d -b -o ""{settings.QobuzFolderPath}/{settings.saveLayout.qobuzAlbum}""";
                }

                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                startInfo.StandardOutputEncoding = Encoding.UTF8; // Set the encoding to UTF8

                // Acquire the semaphore slot here, just before starting the download process
                await downloadSemaphore.WaitAsync();
                activeDownloads++;
                UpdateDownloadBadge();

                Process process = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };

                // Assign the process to the DownloadItem
                downloadItem.DownloadProcess = process;

                int totalCount = 0; // Declare totalCount variable here

                process.OutputDataReceived += (s, output) =>
                {
                    if (!string.IsNullOrEmpty(output.Data))
                    {
                        string outputMessage = output.Data;

                        if (outputMessage.StartsWith("✔ Path:"))
                        {
                            // Extract the path for the individual track
                            string trackPath = outputMessage.Substring("✔ Path:".Length).Trim();
                            // Store the first track path if none has been stored yet
                            if (string.IsNullOrEmpty(downloadItem.DownloadPath))
                            {
                                downloadItem.DownloadPath = trackPath;
                            }
                        }
                        else if (outputMessage.StartsWith("ℹ info Saved in:"))
                        {
                            // Set flag to capture the album path on the next line
                            captureNextLineAsAlbumPath = true;
                        }
                        else if (captureNextLineAsAlbumPath)
                        {
                            // Capture the album path
                            string albumPath = outputMessage.Trim();
                            // Store the album path
                            downloadItem.AlbumDownloadPath = albumPath;
                            captureNextLineAsAlbumPath = false;
                        }
                        // Check if the output message contains any of the previous status messages
                        if (outputMessage.Contains("ℹ info") ||
                            outputMessage.Contains("⚠ warn") ||
                            outputMessage.Contains("● pending") ||
                            outputMessage.Contains("✔ success") ||
                            outputMessage.Contains("✔ Path:") ||
                            outputMessage.Contains("✖ error"))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                string cleanedPathOutputMessage = RemoveAnsiEscapeSequences(outputMessage);
                                downloadItem.RawOutput += cleanedPathOutputMessage + Environment.NewLine;
                                // Extract the status information from the output message
                                if (outputMessage.Contains("✖ error"))
                                {
                                    downloadItem.Status = "Error";
                                    downloadItem.ErrorMessage = outputMessage;
                                }
                                else if (outputMessage.Contains("✔ success"))
                                {
                                    downloadItem.Status = "Completed";
                                    // Update the call to use downloadManagerControl
                                    if (downloadManagerControl != null)
                                    {
                                        downloadManagerControl.UpdateDownloadItemProgress(downloadItem, 1, 1); // Set progress to 100% when completed
                                    }
                                }
                                // Check and append path information to the Log
                                else if (outputMessage.StartsWith("✔ Path:"))
                                {
                                    string pathMessage = cleanedPathOutputMessage.Substring("✔ Path:".Length).Trim();
                                    downloadItem.Log += "Path: " + pathMessage + Environment.NewLine;
                                }
                                else if (outputMessage.Contains("● pending"))
                                {
                                    downloadItem.Status = "In progress";
                                }
                                else if (outputMessage.Contains("⚠ warn"))
                                {
                                    downloadItem.Status = "Warning";
                                }
                                else if (outputMessage.Contains("ℹ info") && outputMessage.Contains("(") && outputMessage.Contains("/"))
                                {
                                    int currentIndex = 0;
                                    totalCount = 0;

                                    // Extract the current and total count from the output message
                                    int startIndex = outputMessage.IndexOf("(") + 1;
                                    int endIndex = outputMessage.IndexOf("/");
                                    if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                                    {
                                        string progress = outputMessage.Substring(startIndex, endIndex - startIndex).Trim();
                                        string[] progressParts = progress.Split('/');
                                        if (progressParts.Length == 2 && int.TryParse(progressParts[0], out currentIndex) && int.TryParse(progressParts[1], out totalCount))
                                        {
                                            // Calculate the progress percentage
                                            double progressPercentage = (double)currentIndex / totalCount;

                                            // Update the download item's progress
                                            downloadManagerWindow.UpdateDownloadItemProgress(downloadItem, currentIndex, totalCount);
                                        }
                                    }
                                }

                                // Remove ANSI escape sequences from the output message
                                string cleanedOutputMessage = RemoveAnsiEscapeSequences(outputMessage);

                                // Update the download item's output
                                downloadItem.RawOutput += cleanedOutputMessage + Environment.NewLine;

                                // Save the relevant lines of output to the log
                                if (cleanedOutputMessage.Contains("✔ success") || cleanedOutputMessage.Contains("✖ error"))
                                {
                                    downloadItem.Log += cleanedOutputMessage + Environment.NewLine;
                                }

                                if (downloadManagerWindow != null)
                                {
                                    // Update the output window from a separate thread
                                    Task.Run(() => downloadManagerWindow.UpdateOutput(cleanedOutputMessage));
                                }

                                // ...

                                string RemoveAnsiEscapeSequences(string input)
                                {
                                    // Regular expression pattern to match ANSI escape sequences
                                    string pattern = @"\x1B\[[^@-~]*[@-~]";

                                    // Remove ANSI escape sequences from the input using regex
                                    string cleanedInput = Regex.Replace(input, pattern, string.Empty);

                                    return cleanedInput;
                                }

                            });
                        }
                    }
                };

                process.Exited += (s, exited) =>
                {
                    downloadItem.EndTime = DateTime.Now;  // Set the end time to now

                    // Check if the process exited successfully
                    if (process.ExitCode == 0)
                    {
                        downloadItem.Status = "Completed";  // If so, set the status to "Completed"
                        db.UpdateDownloadItem(downloadItem);
                    }
                    else
                    {
                        downloadItem.Status = "Failed";  // Otherwise, set it to "Failed"
                        downloadItem.ErrorMessage = downloadItem.Output;  // And save the last output line as the error message
                        db.UpdateDownloadItem(downloadItem);
                    }

                    // Update the download item in the database
                    db.UpdateDownloadItem(downloadItem);

                    if (downloadManagerWindow != null)
                    {
                        downloadManagerWindow.Dispatcher.Invoke(() => downloadManagerWindow.UpdateDownloadItemProgress(downloadItem, totalCount, totalCount));
                    }

                    process.Dispose();
                    downloadSemaphore.Release(); // Release the semaphore slot
                    activeDownloads--;
                    Dispatcher.Invoke(() => UpdateDownloadBadge());
                };

                await Task.Run(() =>
                {
                    process.Start();
                    process.BeginOutputReadLine();
                });
                // downloadItems.Add(downloadItem); // Assuming you have a collection to track download items
            }
            //else
            //{
            //    MessageBox.Show("No valid URL to download.");
            //}
            if (UrlCheckBox.IsChecked == true)
            {
                Search_Box.Text = string.Empty;
            }
        }

        private void ExtractEmbeddedResource(string resourceName, string outputPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException("Resource not found.", resourceName);
                }

                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        private void UrlCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Search.IsEnabled = false;
        }

        private void UrlCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Search.IsEnabled = true;
        }

        private void ManageDownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the downloadManagerWindow object is null
            if (downloadManagerWindow == null)
            {
                db = new DatabaseManager(); // Create an instance of the DatabaseManager
                downloadManagerWindow = new DownloadManagerWindow(db);
            }

            downloadManagerWindow.Show();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                // Pass the console output to the downloadManagerWindow for updating
                downloadManagerWindow?.UpdateOutput(e.Data);
            }
        }

        private MediaPlayer mediaPlayer; // Declare mediaPlayer at the class level

        private void SongsPreview_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the selected SearchResult object
            SearchResult selectedResult = Deezer_Songs_Listbox.SelectedItem as SearchResult;

            if (selectedResult != null)
            {
                string songsPreview = selectedResult.SongsPreview;

                // Stop any currently playing preview
                if (mediaPlayer != null)
                {
                    mediaPlayer.Stop();
                }

                // Perform playback or any other action with the songsPreview URL
                mediaPlayer = new MediaPlayer();
                mediaPlayer.Open(new Uri(songsPreview));
                mediaPlayer.Play();
            }
        }

        private void SongItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Retrieve the selected SearchResult object
            SearchResult selectedResult = Deezer_Songs_Listbox.SelectedItem as SearchResult;

            if (selectedResult != null)
            {
                string songsPreview = selectedResult.SongsPreview;

                // Stop any currently playing preview
                if (mediaPlayer != null)
                {
                    mediaPlayer.Stop();
                }

                // Perform playback or any other action with the songsPreview URL
                mediaPlayer = new MediaPlayer();
                mediaPlayer.Open(new Uri(songsPreview));
                mediaPlayer.Play();
            }
        }

        private string EncodeOutput(string output)
        {
            // Encode special characters to be displayed correctly in the UI
            // You can customize the encoding based on your requirements
            // Here's an example of encoding:
            string encodedOutput = output.Replace("✖", "&#x2716;")
                                         .Replace("✔", "&#x2714;")
                                         .Replace("●", "&#x25CF;")
                                         .Replace("⚠", "&#x26A0;")
                                         .Replace("ℹ", "&#x2139;");

            return encodedOutput;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Button cancelButton = (Button)sender;
            DownloadItem downloadItem = (DownloadItem)cancelButton.DataContext;

            if (downloadProcesses.TryGetValue(downloadItem, out Process process))
            {
                process.Kill();
                process.Dispose();
                downloadProcesses.Remove(downloadItem);
                downloadManagerWindow.DownloadItems.Remove(downloadItem);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var process in downloadProcesses.Values)
            {
                process.Kill();
                process.Dispose();
            }

            downloadProcesses.Clear();
            downloadManagerWindow.DownloadItems.Clear();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow(settings);
            settingsWindow.ShowDialog();
        }

        private async void DownloadAndUnzipDfi()
        {
            string url = "https://github.com/jaylex32/BeatOn/raw/main/d-fi.zip";
            string downloadPath = System.AppDomain.CurrentDomain.BaseDirectory + "d-fi.zip";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                using (FileStream fs = new FileStream(downloadPath, FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }
            }

            // Unzip the downloaded file
            string extractPath = System.AppDomain.CurrentDomain.BaseDirectory;
            ZipFile.ExtractToDirectory(downloadPath, extractPath);

            // Delete the .zip file
            File.Delete(downloadPath);
        }

        private async void Qobuz_Albums_Listbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error reading settings file: {ex.Message}");
                return;
            }

            // Retrieve the selected Album object
            SearchResult selectedAlbum = Qobuz_Albums_Listbox.SelectedItem as SearchResult;

            if (selectedAlbum != null)
            {
                string albumId = selectedAlbum.albumId;

                // Make the API request
                string baseUri = $"https://www.qobuz.com/api.json/0.2/album/get?album_id={albumId}&limit=50";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                    var queryParameters = new Dictionary<string, string>
            {
                { "app_secret", settings.qobuz.secrets.ToString() },
                { "app_id", settings.qobuz.app_id.ToString() }
            };

                    var content = new FormUrlEncodedContent(queryParameters);

                    var response = await client.PostAsync(baseUri, content);
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();

                    JObject data = JObject.Parse(json);

                    if (data == null || data["tracks"] == null)
                    {
                        MessageBox.Show("No data received from the Qobuz API. Please check your search query or API endpoint.");
                        return;
                    }

                    // Clear the existing tracks
                    Qobuz_Songs_Listbox.Items.Clear();
                    Qobuz_Songs_List.Clear();

                    // Add the new tracks from the selected album

                    JArray tracksArray = data["tracks"]["items"] as JArray;

                    foreach (JToken track in tracksArray)
                    {
                        SearchResult result = new SearchResult();

                        if (track == null) continue;  // Skip this iteration if track is null
                        string title = track["title"].ToString();
                        result.Albums = $"{track["performer"]["name"]} - {data["title"]}: {title.Substring(0, Math.Min(60, title.Length))} - {track["maximum_bit_depth"]}bit/{track["maximum_sampling_rate"]}kHz";
                        result.QobuzUrl = $"http://open.qobuz.com/track/{track["id"]}";
                        result.QobuzPoster = data["image"]["large"] != null ? data["image"]["large"].ToString() : null;
                        result.Qposterlabel = $"{track["performer"]["name"]} - {data["title"]}: {track["title"]} ({data["release_date_original"]})";
                        result.Genrelabel = "SONGS";
                        Qobuz_Songs_Listbox.Items.Add(result);
                        Qobuz_Songs_List.Add(result);
                    }

                    // Switch to the Songs Tab
                    MainTabControl.SelectedItem = QobuzTabTabControl;
                    QobuzTabTabControl.SelectedItem = Qobuz_Songs_TabItem;
                }
            }
        }

        private async void Deezer_Albums_Listbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error reading settings file: {ex.Message}");
                return;
            }

            // Retrieve the selected Album object
            SearchResult selectedAlbum = Deezer_Albums_Listbox.SelectedItem as SearchResult;

            if (selectedAlbum != null)
            {
                string albumId = selectedAlbum.albumId;

                // Make the API request for tracks
                string tracksUri = $"https://api.deezer.com/album/{albumId}/tracks";

                // Make the API request for album details
                string albumUri = $"https://api.deezer.com/album/{albumId}";

                using (HttpClient client = new HttpClient())
                {
                    string tracksJson = await client.GetStringAsync(tracksUri);
                    dynamic tracksData = JsonConvert.DeserializeObject(tracksJson);

                    string albumJson = await client.GetStringAsync(albumUri);
                    dynamic albumData = JsonConvert.DeserializeObject(albumJson);

                    if (tracksData == null || tracksData.data == null)
                    {
                        MessageBox.Show("No data received from the Deezer API. Please check your search query or API endpoint.");
                        return;
                    }

                    // Clear the existing tracks
                    Deezer_Songs_Listbox.Items.Clear();
                    songsList.Clear();

                    // Add the new tracks from the selected album
                    JArray tracksArray = tracksData["data"] as JArray;

                    foreach (JToken track in tracksArray)
                    {
                        SearchResult result = new SearchResult();
                        if (track == null) continue;  // Skip this iteration if track is null
                        string trackTitle = track["title"].ToString();
                        if ((bool)track["explicit_lyrics"])  // Check if the track is marked as explicit
                        {
                            trackTitle += " (Explicit)";
                        }
                        result.Albums = $"{track["artist"]["name"]} - {albumData["title"]}: {trackTitle}";
                        result.QobuzUrl = $"{track["link"]}";

                        // Here use the cover URL from the album data
                        result.QobuzPoster = $"{albumData["cover_xl"]}";
                        result.Qposterlabel = $"{track["artist"]["name"]} - {albumData["title"]}: {track["title"]} ({albumData["release_date"]})";
                        result.Genrelabel = "SONGS";
                        result.SongsPreview = $"{track["preview"]}";

                        Deezer_Songs_Listbox.Items.Add(result);
                        songsList.Add(result);
                    }

                    // Switch to the Songs Tab
                    MainTabControl.SelectedItem = DeezerTabTabControl;
                    DeezerTabTabControl.SelectedItem = Songs_TabItem;
                }
            }
        }

        private async void Deezer_Artists_Listbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error reading settings file: {ex.Message}");
                return;
            }

            // Retrieve the selected Artist object
            SearchResult selectedArtist = Deezer_Artist_Listbox.SelectedItem as SearchResult;

            if (selectedArtist != null)
            {
                string artistId = selectedArtist.artistId;

                // Clear the existing albums
                Deezer_Albums_Listbox.Items.Clear();
                albumsList.Clear();

                using (HttpClient client = new HttpClient())
                {
                    for (int i = 0; i < 2; i++)
                    {
                        // Make the API request for artist's albums
                        string artistAlbumsUri = $"https://api.deezer.com/artist/{artistId}/albums?index={i * 50}&limit=50";

                        string json = await client.GetStringAsync(artistAlbumsUri);
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data == null || data.data == null)
                        {
                            MessageBox.Show("No data received from the Deezer API. Please check your search query or API endpoint.");
                            continue;
                        }

                        // Add the new albums from the selected artist
                        JArray albumsArray = data["data"] as JArray;

                        foreach (JToken album in albumsArray)
                        {
                            SearchResult result = new SearchResult();
                            if (album == null) continue;  // Skip this iteration if album is null
                            string albumTitle = album["title"].ToString();
                            if ((bool)album["explicit_lyrics"])  // Check if the track is marked as explicit
                            {
                                albumTitle += " (Explicit)";
                            }
                            result.albumId = $"{album["id"]}";
                            result.Albums = $"{selectedArtist.Albums} - {albumTitle}";  // Use selectedArtist's name
                            result.QobuzUrl = $"{album["link"]}";
                            result.QobuzPoster = $"{album["cover_xl"]}";
                            result.Qposterlabel = $"{selectedArtist.Albums} - {album["title"]} ({album["release_date"]})";
                            result.Genrelabel = "ALBUMS";

                            Deezer_Albums_Listbox.Items.Add(result);
                            albumsList.Add(result);
                        }
                    }

                    // Switch to the Albums Tab
                    MainTabControl.SelectedItem = DeezerTabTabControl;
                    DeezerTabTabControl.SelectedItem = Albums_TabItem;
                }
            }
        }

        private async void Qobuz_Artist_Listbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error reading settings file: {ex.Message}");
                return;
            }

            // Retrieve the selected Album object
            SearchResult selectedArtist = Qobuz_Artist_Listbox.SelectedItem as SearchResult;

            if (selectedArtist != null)
            {
                string artistId = selectedArtist.ArtistId;

                // Make the API request
                string baseUri = $"https://www.qobuz.com/api.json/0.2/artist/get?artist_id={artistId}&extra=albums&limit=50";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                    var queryParameters = new Dictionary<string, string>
            {
                { "app_secret", settings.qobuz.secrets.ToString() },
                { "app_id", settings.qobuz.app_id.ToString() }
            };

                    var content = new FormUrlEncodedContent(queryParameters);

                    var response = await client.PostAsync(baseUri, content);
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();

                    JObject data = JObject.Parse(json);

                    if (data == null || data["albums"] == null)
                    {
                        MessageBox.Show("No data received from the Qobuz API. Please check your search query or API endpoint.");
                        return;
                    }

                    // Clear the existing tracks
                    Qobuz_Albums_Listbox.Items.Clear();
                    Qobuz_Albums_List.Clear();

                    // Add the new tracks from the selected album

                    JArray albumsArray = data["albums"]["items"] as JArray;

                    foreach (JToken album in albumsArray)
                    {
                        SearchResult result = new SearchResult();

                        if (album == null) continue;  // Skip this iteration if track is null
                        string title = album["title"].ToString();
                        result.Albums = $"{album["artist"]["name"]} - {title.Substring(0, Math.Min(60, title.Length))} - {album["maximum_bit_depth"]}bit/{album["maximum_sampling_rate"]}kHz";
                        result.QobuzUrl = $"http://open.qobuz.com/album/{album["id"]}";
                        result.QobuzPoster = album["image"]["large"] != null ? album["image"]["large"].ToString() : null;
                        result.Qposterlabel = $"{album["artist"]["name"]} - {album["title"]}: {album["title"]} ({album["release_date_original"]})";
                        result.Genrelabel = "ALBUMS";
                        result.albumId = $"{album["id"]}";
                        Qobuz_Albums_Listbox.Items.Add(result);
                        Qobuz_Albums_List.Add(result);
                    }

                    // Switch to the Albums Tab
                    MainTabControl.SelectedItem = QobuzTabTabControl;
                    QobuzTabTabControl.SelectedItem = Qobuz_Albums_TabItem;
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (var process in downloadProcesses.Values)
            {
                process.CancelOutputRead(); // Cancel output reading to prevent accessing disposed process
                process.Close(); // Close the process gracefully
            }
        }

        private async void ContextMenuDownload_Click(object sender, RoutedEventArgs e)
        {
            var dbFileName = "downloads.db";
            var dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);

            if (!File.Exists(dbFilePath))
            {
                try
                {
                    //Debug.WriteLine("Database file not found during download operation. Attempting to extract from resources.");
                    ExtractEmbeddedResource("WpfApp1.downloads.db", dbFilePath);
                    //Debug.WriteLine("Successfully extracted database file.");

                    // Introduce a delay to ensure the file is completely written
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to recreate the database. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            SearchResult selectedSearchResult = null;

            // Check which main tab is selected (Deezer or Qobuz)
            if (MainTabControl.SelectedIndex == 0) // Deezer tab is selected
            {
                // Check which Deezer sub-tab is selected and get the selected item
                if (DeezerTabTabControl.SelectedItem == Artist_TabItem)
                    selectedSearchResult = Deezer_Artist_Listbox.SelectedItem as SearchResult;
                else if (DeezerTabTabControl.SelectedItem == Songs_TabItem)
                    selectedSearchResult = Deezer_Songs_Listbox.SelectedItem as SearchResult;
                else if (DeezerTabTabControl.SelectedItem == Albums_TabItem)
                    selectedSearchResult = Deezer_Albums_Listbox.SelectedItem as SearchResult;
                else if (DeezerTabTabControl.SelectedItem == Playlist_TabItem)
                    selectedSearchResult = Deezer_Playlist_Listbox.SelectedItem as SearchResult;
            }
            else if (MainTabControl.SelectedIndex == 1) // Qobuz tab is selected
            {
                // Check which Qobuz sub-tab is selected and get the selected item
                if (QobuzTabTabControl.SelectedItem == Qobuz_Artist_TabItem)
                    try
                    {
                        selectedSearchResult = Qobuz_Artist_Listbox.SelectedItem as SearchResult;
                        if (selectedSearchResult != null)
                        {
                            await DownloadAllAlbumsForArtist(selectedSearchResult.ArtistId);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                else if (QobuzTabTabControl.SelectedItem == Qobuz_Songs_TabItem)
                    selectedSearchResult = Qobuz_Songs_Listbox.SelectedItem as SearchResult;
                else if (QobuzTabTabControl.SelectedItem == Qobuz_Albums_TabItem)
                    selectedSearchResult = Qobuz_Albums_Listbox.SelectedItem as SearchResult;
                // Add more cases as needed for other Qobuz sub-tabs
            }

            // Initiate the download if a search result is selected
            if (selectedSearchResult != null)
            {
                await StartDownloadFromSearchResult(selectedSearchResult);
            }
        }

        private async Task StartDownloadFromSearchResult(SearchResult searchResult)
        {
            if (searchResult == null) return;

            // Use the provided SearchResult's URL for the download
            string urlToDownload = searchResult.QobuzUrl;
            if (string.IsNullOrEmpty(urlToDownload)) return;

            // Prepare the DownloadItem
            DownloadItem downloadItem = new DownloadItem
            {
                Name = searchResult.Albums,
                Progress = 0,
                StartTime = DateTime.Now,
                Url = urlToDownload,
                Status = "Waiting",
                SourceType = MainTabControl.SelectedIndex == 0 ? "Deezer" : "Qobuz"
            };

            // Save the download item using the database manager
            db.SaveDownloadItem(downloadItem); // Assuming db is your DatabaseManager instance

            // Adding the download item to the DownloadManagerWindow
            downloadManagerControl.AddDownloadItem(downloadItem); // Assuming downloadManagerWindow is your DownloadManagerWindow instance

            string qobuzQualityValue = MapQualityToValue(settings.QobuzQuality);
            // Setup the download process
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = @".\d-fi.exe",
                Arguments = MainTabControl.SelectedIndex == 0
                    ? $@"/c -u '{urlToDownload}' -q ""{settings.DeezerQuality}"" -o ""{settings.DeezerFolderPath}/{settings.saveLayout.album}"" -d"
                    : $@"/c -u '{urlToDownload}' -q ""{qobuzQualityValue}"" -d -b -o ""{settings.QobuzFolderPath}/{settings.saveLayout.qobuzAlbum}""",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            // Start the download process
            await downloadSemaphore.WaitAsync();
            activeDownloads++;
            UpdateDownloadBadge();

            Process process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            downloadItem.DownloadProcess = process;
            int totalCount = 0; // Declare totalCount here

            process.OutputDataReceived += (s, output) =>
            {
                if (!string.IsNullOrEmpty(output.Data))
                {
                    string outputMessage = output.Data;

                    if (outputMessage.StartsWith("✔ Path:"))
                    {
                        // Extract the path for the individual track
                        string trackPath = outputMessage.Substring("✔ Path:".Length).Trim();
                        // Store the first track path if none has been stored yet
                        if (string.IsNullOrEmpty(downloadItem.DownloadPath))
                        {
                            downloadItem.DownloadPath = trackPath;
                        }
                    }
                    else if (outputMessage.StartsWith("ℹ info Saved in:"))
                    {
                        // Set flag to capture the album path on the next line
                        captureNextLineAsAlbumPath = true;
                    }
                    else if (captureNextLineAsAlbumPath)
                    {
                        // Capture the album path
                        string albumPath = outputMessage.Trim();
                        // Store the album path
                        downloadItem.AlbumDownloadPath = albumPath;
                        captureNextLineAsAlbumPath = false;
                    }

                    // Check if the output message contains any of the previous status messages
                    if (outputMessage.Contains("ℹ info") ||
                        outputMessage.Contains("⚠ warn") ||
                        outputMessage.Contains("● pending") ||
                        outputMessage.Contains("✔ success") ||
                        outputMessage.Contains("✔ Path:") ||
                        outputMessage.Contains("✖ error"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            string cleanedRightClickOutputMessage = RemoveAnsiEscapeSequences(outputMessage);
                            // Extract the status information from the output message
                            if (outputMessage.Contains("✖ error"))
                            {
                                downloadItem.Status = "Error";
                                downloadItem.ErrorMessage = outputMessage;
                            }
                            else if (outputMessage.Contains("✔ success"))
                            {
                                downloadItem.Status = "Completed";
                                // Update the call to use downloadManagerControl
                                if (downloadManagerControl != null)
                                {
                                    downloadManagerControl.UpdateDownloadItemProgress(downloadItem, 1, 1); // Set progress to 100% when completed
                                }
                            }
                            // Check and append path information to the Log
                            else if (outputMessage.StartsWith("✔ Path:"))
                            {
                                string pathMessage = cleanedRightClickOutputMessage.Substring("✔ Path:".Length).Trim();
                                downloadItem.Log += "Path: " + pathMessage + Environment.NewLine;
                            }
                            else if (outputMessage.Contains("● pending"))
                            {
                                downloadItem.Status = "In progress";
                            }
                            else if (outputMessage.Contains("⚠ warn"))
                            {
                                downloadItem.Status = "Warning";
                            }
                            else if (outputMessage.Contains("ℹ info") && outputMessage.Contains("(") && outputMessage.Contains("/"))
                            {
                                int currentIndex = 0;
                                totalCount = 0;

                                // Extract the current and total count from the output message
                                int startIndex = outputMessage.IndexOf("(") + 1;
                                int endIndex = outputMessage.IndexOf("/");
                                if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                                {
                                    string progress = outputMessage.Substring(startIndex, endIndex - startIndex).Trim();
                                    string[] progressParts = progress.Split('/');
                                    if (progressParts.Length == 2 && int.TryParse(progressParts[0], out currentIndex) && int.TryParse(progressParts[1], out totalCount))
                                    {
                                        // Calculate the progress percentage
                                        double progressPercentage = (double)currentIndex / totalCount;

                                        // Update the download item's progress
                                        downloadManagerWindow.UpdateDownloadItemProgress(downloadItem, currentIndex, totalCount);
                                    }
                                }
                            }

                            // Remove ANSI escape sequences from the output message
                            string cleanedOutputMessage = RemoveAnsiEscapeSequences(outputMessage);

                            // Update the download item's output
                            downloadItem.RawOutput += cleanedOutputMessage + Environment.NewLine;

                            // Save the relevant lines of output to the log
                            if (cleanedOutputMessage.Contains("✔ success") || cleanedOutputMessage.Contains("✖ error"))
                            {
                                downloadItem.Log += cleanedOutputMessage + Environment.NewLine;
                            }

                            if (downloadManagerWindow != null)
                            {
                                // Update the output window from a separate thread
                                Task.Run(() => downloadManagerWindow.UpdateOutput(cleanedOutputMessage));
                            }

                            // ...

                            string RemoveAnsiEscapeSequences(string input)
                            {
                                // Regular expression pattern to match ANSI escape sequences
                                string pattern = @"\x1B\[[^@-~]*[@-~]";

                                // Remove ANSI escape sequences from the input using regex
                                string cleanedInput = Regex.Replace(input, pattern, string.Empty);

                                return cleanedInput;
                            }

                        });
                    }
                }
            };

            process.Exited += (s, exited) =>
            {
                downloadItem.EndTime = DateTime.Now;  // Set the end time to now

                // Check if the process exited successfully
                if (process.ExitCode == 0)
                {
                    downloadItem.Status = "Completed";  // If so, set the status to "Completed"
                    db.UpdateDownloadItem(downloadItem);
                }
                else
                {
                    downloadItem.Status = "Failed";  // Otherwise, set it to "Failed"
                    downloadItem.ErrorMessage = downloadItem.Output;  // And save the last output line as the error message
                    db.UpdateDownloadItem(downloadItem);
                }

                // Update the download item in the database
                db.UpdateDownloadItem(downloadItem);

                if (downloadManagerWindow != null)
                {
                    downloadManagerWindow.Dispatcher.Invoke(() => downloadManagerWindow.UpdateDownloadItemProgress(downloadItem, totalCount, totalCount));
                }

                process.Dispose();
                downloadSemaphore.Release(); // Release the semaphore slot
                activeDownloads--;
                Dispatcher.Invoke(() => UpdateDownloadBadge());
            };

            await Task.Run(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
            });
        }
        private async Task DownloadAllAlbumsForArtist(string artistId)
        {
            try
            {
                string json = File.ReadAllText("d-fi.config.json");
                settings = JsonConvert.DeserializeObject<Settings>(json);

                string baseUri = $"https://www.qobuz.com/api.json/0.2/artist/get?artist_id={artistId}&extra=albums&limit=50";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    var queryParameters = new Dictionary<string, string>
            {
                { "app_secret", settings.qobuz.secrets.ToString() },
                { "app_id", settings.qobuz.app_id.ToString() }
            };

                    var content = new FormUrlEncodedContent(queryParameters);
                    var response = await client.PostAsync(baseUri, content);
                    response.EnsureSuccessStatusCode();

                    json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    if (data == null || data["albums"] == null)
                    {
                        MessageBox.Show("No data received from the Qobuz API. Please check your search query or API endpoint.");
                        return;
                    }

                    JArray albumsArray = data["albums"]["items"] as JArray;
                    foreach (JToken album in albumsArray)
                    {
                        SearchResult albumResult = new SearchResult
                        {
                            // Populate albumResult properties from the album JToken
                            Albums = $"{album["artist"]["name"]} - {album["title"]}",
                            QobuzUrl = $"http://open.qobuz.com/album/{album["id"]}",
                            // ... Add other properties as needed
                        };

                        // Start the download for this album
                        await StartDownloadFromSearchResult(albumResult);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during album download: {ex.Message}");
            }
        }

        private async void ArtistDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var dbFileName = "downloads.db";
            var dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);

            if (!File.Exists(dbFilePath))
            {
                try
                {
                    //Debug.WriteLine("Database file not found during download operation. Attempting to extract from resources.");
                    ExtractEmbeddedResource("WpfApp1.downloads.db", dbFilePath);
                    //Debug.WriteLine("Successfully extracted database file.");

                    // Introduce a delay to ensure the file is completely written
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to recreate the database. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }


            try
            {
                SearchResult selectedArtist = Qobuz_Artist_Listbox.SelectedItem as SearchResult;
                if (selectedArtist != null)
                {
                    await DownloadAllAlbumsForArtist(selectedArtist.ArtistId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void ContextMenuLink_Click(object sender, RoutedEventArgs e)
        {
            // Assuming sender is MenuItem and its DataContext is SearchResult
            MenuItem menuItem = sender as MenuItem;
            SearchResult selectedSearchResult = menuItem.DataContext as SearchResult;
            if (selectedSearchResult != null && !string.IsNullOrEmpty(selectedSearchResult.QobuzUrl))
            {
                // Open the URL in the default web browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = selectedSearchResult.QobuzUrl,
                    UseShellExecute = true
                });
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //downloadManagerWindow.Close();
            foreach (var process in downloadProcesses.Values)
            {               
                process.CancelOutputRead(); // Cancel output reading to prevent accessing disposed process
                process.Close(); // Close the process gracefully
            }
            Application.Current.Shutdown();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl)
            {
                if (MainTabControl.SelectedItem == Deezer_TabItem)
                {
                    UpdateSearchControlsStateBasedOnDeezerTabControl();
                }
                else if (MainTabControl.SelectedItem == Qobuz_TabItem)
                {
                    UpdateSearchControlsStateBasedOnQobuzTabControl();
                }
                else
                {
                    EnableSearchControls();
                }
            }
        }

        private void SubTabTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
        private void QobuzTabTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl)
            {
                UpdateSearchControlsStateBasedOnQobuzTabControl();
            }
        }

        private void UpdateSearchControlsStateBasedOnMainTabControl()
        {
            if (MainTabControl.SelectedItem == Qobuz_TabItem)
            {
                UpdateSearchControlsStateBasedOnQobuzTabControl();
            }
            else
            {
                EnableSearchControls();
            }
        }

        private void UpdateSearchControlsStateBasedOnQobuzTabControl()
        {
            // Check if QobuzTabTabControl and its SelectedItem are not null
            if (QobuzTabTabControl != null && QobuzTabTabControl.SelectedItem != null)
            {
                var isFeaturedOrGenreTabSelected = QobuzTabTabControl.SelectedIndex == 3 || QobuzTabTabControl.SelectedIndex == 4;

                // Toggle visibility of search controls and labels
                Search_Box.Visibility = isFeaturedOrGenreTabSelected ? Visibility.Collapsed : Visibility.Visible;
                Search.Visibility = isFeaturedOrGenreTabSelected ? Visibility.Collapsed : Visibility.Visible;
                UrlCheckBox.Visibility = isFeaturedOrGenreTabSelected ? Visibility.Collapsed : Visibility.Visible;

                // Toggle label visibility based on the selected tab
                FeaturedLabelBorder.Visibility = QobuzTabTabControl.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
                GenreLabelBorder.Visibility = QobuzTabTabControl.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
                DeezerFeaturedLabelBorder.Visibility = DeezerTabTabControl.SelectedIndex == 4 ? Visibility.Hidden : Visibility.Collapsed;
                ChartLabelBorder.Visibility = DeezerTabTabControl.SelectedIndex == 5 ? Visibility.Hidden : Visibility.Collapsed;
            }
            else
            {
                // Default to enabling the controls if QobuzTabTabControl is not initialized
                EnableSearchControls();
            }
        }

        private void DeezerTabTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl)
            {
                UpdateSearchControlsStateBasedOnDeezerTabControl();
            }
        }

        private void UpdateSearchControlsStateBasedOnDeezerTabControl()
        {
            if (DeezerTabTabControl != null && DeezerTabTabControl.SelectedItem != null)
            {
                var isFeaturedorchartTabSelected = DeezerTabTabControl.SelectedIndex == 4 || DeezerTabTabControl.SelectedIndex == 5;
                Search_Box.Visibility = isFeaturedorchartTabSelected ? Visibility.Collapsed : Visibility.Visible;
                Search.Visibility = isFeaturedorchartTabSelected ? Visibility.Collapsed : Visibility.Visible;
                UrlCheckBox.Visibility = isFeaturedorchartTabSelected ? Visibility.Collapsed : Visibility.Visible;
                DeezerFeaturedLabelBorder.Visibility = DeezerTabTabControl.SelectedIndex == 4 ? Visibility.Visible : Visibility.Collapsed;
                ChartLabelBorder.Visibility = DeezerTabTabControl.SelectedIndex == 5 ? Visibility.Visible : Visibility.Collapsed;
                FeaturedLabelBorder.Visibility = QobuzTabTabControl.SelectedIndex == 3 ? Visibility.Hidden : Visibility.Collapsed;
                GenreLabelBorder.Visibility = QobuzTabTabControl.SelectedIndex == 4 ? Visibility.Hidden : Visibility.Collapsed;
            }
            else
            {
                EnableSearchControls();
            }
        }

        private void EnableSearchControls()
        {
            if (Search_Box != null)
            {
                Search_Box.Visibility = Visibility.Visible;
            }

            if (Search != null)
            {
                Search.Visibility = Visibility.Visible;
            }

            if (UrlCheckBox != null)
            {
                UrlCheckBox.Visibility = Visibility.Visible;
            }
        }


        public class DeezerFeaturedAlbum
        {
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Cover { get; set; }
            public string DeezerUrl { get; set; }
        }

        public class DeezerFeaturedSection
        {
            public string SectionName { get; set; }
            public List<DeezerFeaturedAlbum> Albums { get; set; }
        }

        private async void DeezerContainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedComboBoxItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedComboBoxItem == null) return;

            string selectedGenre = selectedComboBoxItem.Content.ToString();
            string apiEndpoint = "";
            
            if (selectedGenre == "None")
            {
                if (Deezer_Featured_SectionsControl != null)
                {
                    Dispatcher.Invoke(() => Deezer_Featured_SectionsControl.ItemsSource = null);
                }
                return;
            }

            switch (selectedGenre)
            {
                case "Reggaeton":
                    apiEndpoint = "https://api.deezer.com/editorial/122/releases?&limit=35";
                    break;
                case "Pop":
                    apiEndpoint = "https://api.deezer.com/editorial/132/releases?&limit=35";
                    break;
                case "Rock":
                    apiEndpoint = "https://api.deezer.com/editorial/152/releases?&limit=35";
                    break;
                case "Rap/Hip Hop":
                    apiEndpoint = "https://api.deezer.com/editorial/116/releases?&limit=35";
                    break;
                case "Dance":
                    apiEndpoint = "https://api.deezer.com/editorial/113/releases?&limit=35";
                    break;
                case "RNB":
                    apiEndpoint = "https://api.deezer.com/editorial/165/releases?&limit=35";
                    break;
                case "Alternative":
                    apiEndpoint = "https://api.deezer.com/editorial/85/releases?&limit=35";
                    break;
                case "Christian":
                    apiEndpoint = "https://api.deezer.com/editorial/186/releases?&limit=35";
                    break;
                case "Electro":
                    apiEndpoint = "https://api.deezer.com/editorial/106/releases?&limit=35";
                    break;
                case "Reggae":
                    apiEndpoint = "https://api.deezer.com/editorial/144/releases?&limit=35";
                    break;
                case "Salsa":
                    apiEndpoint = "https://api.deezer.com/editorial/67/releases?&limit=35";
                    break;
            } 

            if (string.IsNullOrEmpty(apiEndpoint)) return;

            try
            {
                using var client = new HttpClient();
                string json = await client.GetStringAsync(apiEndpoint);
                dynamic data = JsonConvert.DeserializeObject(json);

                var deezerFeaturedAlbums = new List<DeezerFeaturedAlbum>();
                foreach (var item in data.data)
                {
                    string artistalbum = $"{item.artist.name} - {item.title}";
                    deezerFeaturedAlbums.Add(new DeezerFeaturedAlbum
                    {
                        Title = artistalbum,
                        Artist = item.artist.name,
                        Cover = item.cover_medium,
                        DeezerUrl = "https://api.deezer.com/album/" + item.id
                    });
                }

                var deezerFeaturedSection = new DeezerFeaturedSection
                {
                    SectionName = "Featured Releases",
                    Albums = deezerFeaturedAlbums
                };

                Dispatcher.Invoke(() =>
                {
                    Deezer_Featured_SectionsControl.ItemsSource = new List<DeezerFeaturedSection> { deezerFeaturedSection };
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching data: {ex.Message}");
            }
        }
        private async void DeezerDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var dbFileName = "downloads.db";
            var dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);

            if (!File.Exists(dbFilePath))
            {
                try
                {
                    //Debug.WriteLine("Database file not found during download operation. Attempting to extract from resources.");
                    ExtractEmbeddedResource("WpfApp1.downloads.db", dbFilePath);
                    //Debug.WriteLine("Successfully extracted database file.");

                    // Introduce a delay to ensure the file is completely written
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to recreate the database. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var button = sender as Button;
            var album = button.Tag as DeezerFeaturedAlbum;

            if (album != null)
            {  
                string albumUrl = album.DeezerUrl;
                SearchResult searchResult = new SearchResult
                {
                    Albums = album.Title,
                    QobuzUrl = albumUrl // Note: This property name might need to be updated to be more generic
                };

                // Now call the existing download logic
                await StartDownloadFromSearchResult(searchResult);
            }
        }

        public class DeezerChartAlbum
        {
            public string Title { get; set; }
            public string Artist { get; set; }
            public string Cover { get; set; }
            public string DeezerUrl { get; set; }
        }

        public class DeezerChartSection
        {
            public string SectionName { get; set; }
            public List<DeezerChartAlbum> Albums { get; set; }
        }

        private async void DeezerChartContainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            ComboBoxItem selectedComboBoxItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedComboBoxItem == null) return;

            string selectedGenre = selectedComboBoxItem.Content.ToString();
            string apiEndpoint = "";

            if (selectedGenre == "None")
            {
                if (Deezer_Chart_SectionsControl != null)
                {
                    Dispatcher.Invoke(() => Deezer_Chart_SectionsControl.ItemsSource = null);
                }
                return;
            }

            switch (selectedGenre)
            {
                case "Reggaeton":
                    apiEndpoint = "https://api.deezer.com/chart/122/albums?&limit=35";
                    break;
                case "Pop":
                    apiEndpoint = "https://api.deezer.com/chart/132/albums?&limit=35";
                    break;
                case "Rock":
                    apiEndpoint = "https://api.deezer.com/chart/152/albums?&limit=35";
                    break;
                case "Rap/Hip Hop":
                    apiEndpoint = "https://api.deezer.com/chart/116/albums?&limit=35";
                    break;
                case "Dance":
                    apiEndpoint = "https://api.deezer.com/chart/113/albums?&limit=35";
                    break;
                case "RNB":
                    apiEndpoint = "https://api.deezer.com/chart/165/albums?&limit=35";
                    break;
                case "Alternative":
                    apiEndpoint = "https://api.deezer.com/chart/85/albums?&limit=35";
                    break;
                case "Christian":
                    apiEndpoint = "https://api.deezer.com/chart/186/albums?&limit=35";
                    break;
                case "Electro":
                    apiEndpoint = "https://api.deezer.com/chart/106/albums?&limit=35";
                    break;
                case "Reggae":
                    apiEndpoint = "https://api.deezer.com/chart/144/albums?&limit=35";
                    break;
                case "Salsa":
                    apiEndpoint = "https://api.deezer.com/chart/67/albums?&limit=35";
                    break;
            }

            if (string.IsNullOrEmpty(apiEndpoint)) return;

            try
            {
                using var client = new HttpClient();
                string json = await client.GetStringAsync(apiEndpoint);
                dynamic data = JsonConvert.DeserializeObject(json);

                var deezerChartAlbums = new List<DeezerChartAlbum>();
                foreach (var item in data.data)
                {
                    string artistalbum = $"{item.artist.name} - {item.title}";
                    deezerChartAlbums.Add(new DeezerChartAlbum
                    {
                        Title = artistalbum,
                        Artist = item.artist.name,
                        Cover = item.cover_medium,
                        DeezerUrl = item.link
                    });
                }

                var deezerChartSection = new DeezerChartSection
                {
                    SectionName = "Chart Albums",
                    Albums = deezerChartAlbums
                };

                Dispatcher.Invoke(() =>
                {
                    Deezer_Chart_SectionsControl.ItemsSource = new List<DeezerChartSection> { deezerChartSection };
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching data: {ex.Message}");
            }
        }
        private async void DeezerChartDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var dbFileName = "downloads.db";
            var dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);

            if (!File.Exists(dbFilePath))
            {
                try
                {
                    //Debug.WriteLine("Database file not found during download operation. Attempting to extract from resources.");
                    ExtractEmbeddedResource("WpfApp1.downloads.db", dbFilePath);
                    //Debug.WriteLine("Successfully extracted database file.");

                    // Introduce a delay to ensure the file is completely written
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to recreate the database. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }



            var button = sender as Button;
            var album = button.Tag as DeezerChartAlbum;

            if (album != null)
            {
                string albumUrl = album.DeezerUrl;
                SearchResult searchResult = new SearchResult
                {
                    Albums = album.Title,
                    QobuzUrl = albumUrl // Note: This property name might need to be updated to be more generic
                };

                // Now call the existing download logic
                await StartDownloadFromSearchResult(searchResult);
            }
        }

    }
}