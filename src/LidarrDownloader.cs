using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Dynamic;
using System.IO;
using FuzzySharp;
using System.Windows;
using System.Net.Http.Headers;
using WpfApp1;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

public class LidarrDownloader
{
    private readonly string lidarrApiKey;
    private readonly string lidarrBaseUrl;
    private readonly AlbumCache albumCache;
    private readonly LidarrDownloaderStats _stats;
    private static readonly Regex CleanRegex = new Regex(@"[^\w\s]|\(.*?\)|\[.*?\]", RegexOptions.Compiled);
    private static readonly string[] CommonWords = { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by" };

    private string LidarrApiUrl => $"{lidarrBaseUrl}/api/v1";
    private readonly bool lidarrEnabled;

    private readonly DownloadManagerControl downloadManagerControl;
    private readonly DatabaseManager db;
    private readonly Dispatcher dispatcher;
    private readonly SemaphoreSlim lidarrDownloadSemaphore;
    private readonly dynamic settings;
    private int activeDownloads;
    private bool captureNextLineAsAlbumPath;
    private readonly MainWindow mainWindow;

    private dynamic LoadSettingsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return CreateDefaultSettings();
        }

        string json = File.ReadAllText(filePath);
        dynamic settings = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
        return settings;
    }

    private dynamic CreateDefaultSettings()
    {
        return new ExpandoObject();
    }

    public LidarrDownloader(DownloadManagerControl downloadManagerControl, DatabaseManager db, Dispatcher dispatcher, LidarrDownloaderStats stats, SemaphoreSlim lidarrDownloadSemaphore, Settings settings, MainWindow mainWindow)
    {
        this.lidarrEnabled = settings.LidarrEnabled;
        this.lidarrApiKey = settings.LidarrApiKey;
        this.lidarrBaseUrl = settings.LidarrUrl.TrimEnd('/');
        this.downloadManagerControl = downloadManagerControl;
        this.db = db;
        this.dispatcher = dispatcher;
        this.lidarrDownloadSemaphore = lidarrDownloadSemaphore;
        this.settings = settings;
        this.mainWindow = mainWindow;
        this.activeDownloads = 0;
        this.captureNextLineAsAlbumPath = false;
        _stats = stats;

        string settingsFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "d-fi.config.json");
        this.settings = LoadSettingsFromFile(settingsFilePath);
        string cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "album_cache.txt");
        this.albumCache = new AlbumCache(cacheFilePath);
    }

    private void LogToFile(string message)
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lidarr_debug.log");
        using (StreamWriter writer = File.AppendText(logPath))
        {
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }

    private bool IsValidLidarrUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    private async Task<string> FetchArtistsFromLidarr()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.GetAsync($"{LidarrApiUrl}/Artist/").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }

    private async Task<List<JObject>> FetchWantedAlbumsFromLidarr()
    {
        HashSet<string> processedAlbums = new HashSet<string>();
        List<JObject> allRecords = new List<JObject>();
        int pageSize = 100;
        int page = 1;
        bool hasMore = true;

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            while (hasMore)
            {
                HttpResponseMessage response = await client.GetAsync($"{LidarrApiUrl}/wanted/missing/?page={page}&pageSize={pageSize}&includeArtist=true&monitored=true&sortDir=desc&sortKey=releaseDate").ConfigureAwait(false);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                JObject albums = JObject.Parse(responseBody);
                JArray records = (JArray)albums["records"];
                foreach (JObject record in records)
                {
                    string albumId = record["id"].ToString();
                    if (!processedAlbums.Contains(albumId))
                    {
                        allRecords.Add(record);
                        processedAlbums.Add(albumId);
                    }
                }

                int totalRecords = albums["totalRecords"].Value<int>();
                hasMore = page * pageSize < totalRecords;
                page++;
            }
        }

        return allRecords;
    }

    private async Task<JArray> SearchAlbumInLidarr(string artistName, string albumName)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{LidarrApiUrl}/search?term={Uri.EscapeDataString(artistName + " " + albumName)}").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JArray.Parse(json);
        }
    }

    private bool FuzzyAlbumMatch(string lidarrAlbum, string deezerAlbum, int threshold = 85)
    {
        var cleanLidarr = CleanAlbumName(lidarrAlbum);
        var cleanDeezer = CleanAlbumName(deezerAlbum);

        // Exact match after cleaning
        if (cleanLidarr == cleanDeezer)
            return true;

        // Check if one is a substring of the other
        if (cleanLidarr.Contains(cleanDeezer) || cleanDeezer.Contains(cleanLidarr))
            return true;

        // Fuzzy match using WeightedRatio
        int weightedRatio = Fuzz.WeightedRatio(cleanLidarr, cleanDeezer);
        if (weightedRatio >= threshold)
            return true;

        // Token set ratio for handling word order differences and partial matches
        int tokenSetRatio = Fuzz.TokenSetRatio(cleanLidarr, cleanDeezer);
        if (tokenSetRatio >= threshold)
            return true;

        return false;
    }

    private async Task<(bool exists, int percentComplete)> GetAlbumStatusInLidarr(string artistName, string albumName)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var artistResponse = await client.GetAsync($"{LidarrApiUrl}/artist?artistName={Uri.EscapeDataString(artistName)}").ConfigureAwait(false);
            artistResponse.EnsureSuccessStatusCode();
            var artistJson = await artistResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var artistData = JArray.Parse(artistJson);

            if (artistData.Count == 0)
                return (false, 0);

            var artistId = artistData[0]["id"].ToString();

            var albumResponse = await client.GetAsync($"{LidarrApiUrl}/album?artistId={artistId}").ConfigureAwait(false);
            albumResponse.EnsureSuccessStatusCode();
            var albumJson = await albumResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var albumData = JArray.Parse(albumJson);

            foreach (var album in albumData)
            {
                if (album["title"].ToString().Equals(albumName, StringComparison.OrdinalIgnoreCase))
                {
                    bool monitored = album["monitored"].Value<bool>();
                    int percentOfTracks = album["statistics"]["percentOfTracks"].Value<int>();
                    bool allTracksDownloaded = album["statistics"]["trackFileCount"].Value<int>() == album["statistics"]["totalTrackCount"].Value<int>();

                    if (!monitored && allTracksDownloaded)
                    {
                        return (true, 100);
                    }

                    return (true, percentOfTracks);
                }
            }

            return (false, 0);
        }
    }

    private async Task<(string rootPath, string namingFormat)> GetLidarrFolderStructure()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{LidarrApiUrl}/rootfolder").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var rootFolders = JArray.Parse(json);

            if (rootFolders.Count == 0)
                throw new Exception("No root folders configured in Lidarr");

            var rootPath = rootFolders[0]["path"].ToString();

            var namingResponse = await client.GetAsync($"{LidarrApiUrl}/config/naming").ConfigureAwait(false);
            namingResponse.EnsureSuccessStatusCode();
            var namingJson = await namingResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            var namingConfig = JObject.Parse(namingJson);

            var namingFormat = namingConfig["standardTrackFormat"].ToString();

            return (rootPath, namingFormat);
        }
    }

    public async Task<string> GetDeezerArtistID(string artistName)
    {
        if (string.IsNullOrEmpty(artistName))
        {
            throw new ArgumentNullException(nameof(artistName), "Artist name cannot be null or empty");
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string searchQuery = Uri.EscapeDataString(artistName);
            HttpResponseMessage response = await client.GetAsync($"https://api.deezer.com/search/artist?q={searchQuery}").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var searchResults = JObject.Parse(responseBody);
            return searchResults["data"]?[0]?["id"]?.ToString();
        }
    }

    public async Task<string> QueryAlbumOnDeezer(string deezerArtistID, string albumName, string artistName)
    {
        if (string.IsNullOrEmpty(deezerArtistID) || string.IsNullOrEmpty(albumName))
        {
            return null;
        }

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            int index = 0;
            bool hasMore = true;

            while (hasMore)
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"https://api.deezer.com/artist/{deezerArtistID}/albums?index={index}&limit=100").ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var responseObject = JObject.Parse(responseBody);
                    var albums = responseObject["data"] as JArray;

                    if (albums == null)
                    {
                        return null;
                    }

                    foreach (var album in albums)
                    {
                        string deezerAlbumName = album["title"]?.ToString();
                        if (!string.IsNullOrEmpty(deezerAlbumName))
                        {
                            if (AlbumNamesMatch(albumName, deezerAlbumName) || FuzzyAlbumMatch(albumName, deezerAlbumName))
                            {
                                string albumLink = album["link"]?.ToString();
                                return albumLink;
                            }
                        }
                    }

                    hasMore = responseObject["next"] != null;
                    index += 100;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
    }

    private bool IsAlbumInDownloadQueue(string artistName, string albumName)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                var response = client.GetAsync($"{LidarrApiUrl}/queue").Result;
                response.EnsureSuccessStatusCode();
                var json = response.Content.ReadAsStringAsync().Result;
                var queueObject = JObject.Parse(json);
                var records = (JArray)queueObject["records"];

                if (records == null)
                {
                    return false;
                }

                return records.Any(item =>
                    item["artist"]?["artistName"]?.ToString() == artistName &&
                    item["album"]?["title"]?.ToString() == albumName);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    private bool ShouldSkipAlbumDownload(string artistName, string albumName)
    {
        return downloadManagerControl.DownloadItems.Any(item =>
            item.ArtistName == artistName &&
            item.Name == albumName &&
            (item.Status == "Waiting" || item.Status == "In progress" || item.Status == "Completed"));
    }

    private async Task<string> GetLidarrNamingFormat()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{LidarrApiUrl}/config/naming").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var namingConfig = JObject.Parse(json);

            return namingConfig["standardTrackFormat"].ToString();
        }
    }

    private string CleanAlbumName(string albumName)
    {
        // Remove specific suffixes
        string[] suffixesToRemove = new[] { "(Explicit)", "(Deluxe)", "(Expanded Edition)", "(Remastered)", "(Live)", "(Special Edition)" };
        foreach (var suffix in suffixesToRemove)
        {
            albumName = albumName.Replace(suffix, "", StringComparison.OrdinalIgnoreCase);
        }

        // Remove punctuation, parentheses, and brackets
        albumName = CleanRegex.Replace(albumName, "");

        // Remove common words and trim
        var words = albumName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .Where(word => !CommonWords.Contains(word.ToLower()));

        return string.Join(" ", words).Trim().ToLower();
    }

    private bool AlbumNamesMatch(string lidarrAlbum, string deezerAlbum)
    {
        return CleanAlbumName(lidarrAlbum) == CleanAlbumName(deezerAlbum);
    }

    public async Task<bool> StartDownloadFromDeezer(string albumUrl, string albumName, string artistName, string lidarrRootPath)
    {
        return await StartDownload(albumUrl, albumName, artistName, lidarrRootPath);
    }

    public async Task<bool> StartDownloadFromDeezerUsingSettingsPath(string albumUrl, string albumName, string artistName)
    {
        string settingsPath = settings.DeezerFolderPath; // Assuming this is the correct path from the settings file
        return await StartDownload(albumUrl, albumName, artistName, settingsPath);
    }

    private async Task<bool> StartDownload(string albumUrl, string albumName, string artistName, string downloadPath)
    {
        if (albumCache.IsAlbumProcessed(artistName, albumName))
        {
            LogToFile($"Skipping already processed album: {albumName} by {artistName}");
            return false;
        }

        if (string.IsNullOrEmpty(albumUrl)) return false;

        DownloadItem downloadItem = new DownloadItem
        {
            Name = albumName,
            ArtistName = artistName,
            Progress = 0,
            StartTime = DateTime.Now,
            Url = albumUrl,
            Status = "Waiting",
            SourceType = "Deezer"
        };

        db.SaveDownloadItem(downloadItem);
        await dispatcher.InvokeAsync(() => downloadManagerControl.AddDownloadItem(downloadItem));

        string deezerQualityValue = settings.DeezerQuality;
        string SaveLayout = settings.saveLayout.album;
        string fullPath = System.IO.Path.Combine(downloadPath, SaveLayout);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = @".\d-fi.exe",
            Arguments = $@"/c -u ""{albumUrl}"" -q ""{deezerQualityValue}"" -o ""{fullPath}"" -d",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        await lidarrDownloadSemaphore.WaitAsync().ConfigureAwait(false);
        activeDownloads++;
        UpdateDownloadBadge();

        System.Diagnostics.Process process = new System.Diagnostics.Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        downloadItem.DownloadProcess = process;
        int totalCount = 0;

        bool downloadSuccess = false;

        process.OutputDataReceived += async (s, output) =>
        {
            if (!string.IsNullOrEmpty(output.Data))
            {
                string outputMessage = output.Data;

                if (outputMessage.StartsWith("✔ Path:"))
                {
                    string trackPath = outputMessage.Substring("✔ Path:".Length).Trim();
                    if (string.IsNullOrEmpty(downloadItem.DownloadPath))
                    {
                        downloadItem.DownloadPath = trackPath;
                    }
                }
                else if (outputMessage.StartsWith("ℹ info Saved in:"))
                {
                    captureNextLineAsAlbumPath = true;
                }
                else if (captureNextLineAsAlbumPath)
                {
                    string albumPath = outputMessage.Trim();
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
                    await dispatcher.InvokeAsync(async () =>
                    {
                        string cleanedRightClickOutputMessage = RemoveAnsiEscapeSequences(outputMessage);
                        if (outputMessage.Contains("✖ error"))
                        {
                            downloadItem.Status = "Error";
                            downloadItem.ErrorMessage = outputMessage;
                        }
                        else if (outputMessage.Contains("✔ success") || outputMessage.Contains("ℹ info Saved in:"))
                        {
                            downloadSuccess = true;
                            downloadItem.Status = "Completed";
                            await UpdateDownloadStatus(albumName, artistName, "Completed");
                        }
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
                            int totalCount = 0;
                            int startIndex = outputMessage.IndexOf("(") + 1;
                            int endIndex = outputMessage.IndexOf("/");
                            if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                            {
                                string progress = outputMessage.Substring(startIndex, endIndex - startIndex).Trim();
                                string[] progressParts = progress.Split('/');
                                if (progressParts.Length == 2 && int.TryParse(progressParts[0], out currentIndex) && int.TryParse(progressParts[1], out totalCount))
                                {
                                    double progressPercentage = (double)currentIndex / totalCount;
                                    int progressInt = (int)(progressPercentage * 100);
                                    downloadItem.Progress = progressInt;
                                    downloadItem.Status = progressInt == 100 ? "Completed" : "In progress";

                                    await dispatcher.InvokeAsync(() => {
                                        downloadManagerControl.UpdateDownloadItemProgress(downloadItem, currentIndex, totalCount);
                                    });

                                    db.UpdateDownloadItem(downloadItem);
                                    _stats.UpdateDownloadProgress(downloadItem.ArtistName, downloadItem.Name, progressInt);

                                    if (progressInt == 100)
                                    {
                                        await HandleDownloadCompletion(downloadItem);
                                    }
                                }
                            }
                        }

                        string cleanedOutputMessage = RemoveAnsiEscapeSequences(outputMessage);
                        downloadItem.RawOutput += cleanedOutputMessage + Environment.NewLine;

                        if (cleanedOutputMessage.Contains("✔ success") || cleanedOutputMessage.Contains("✖ error"))
                        {
                            downloadItem.Log += cleanedOutputMessage + Environment.NewLine;
                        }

                        if (downloadManagerControl != null)
                        {
                            Task.Run(() => downloadManagerControl.UpdateOutput(cleanedOutputMessage));
                        }

                        UpdateDownloadStatus(albumName, artistName, downloadItem.Status);
                    });
                }
            }
        };

        process.Exited += async (s, exited) =>
        {
            await Task.Delay(500); // Small delay to ensure all output is processed

            downloadItem.EndTime = DateTime.Now;

            if (!downloadSuccess)
            {
                if (process.ExitCode == 0)
                {
                    downloadSuccess = true;
                    await UpdateDownloadStatus(albumName, artistName, "Completed");
                }
                else
                {
                    await UpdateDownloadStatus(albumName, artistName, "Failed");
                    _stats.AddDownload(albumName, artistName, "Failed");
                }
            }

            db.UpdateDownloadItem(downloadItem);

            await dispatcher.InvokeAsync(() =>
            {
                downloadManagerControl.UpdateDownloadItemProgress(downloadItem, totalCount, totalCount);
            });

            process.Dispose();
            lidarrDownloadSemaphore.Release();
            activeDownloads--;
            await dispatcher.InvokeAsync(() => UpdateDownloadBadge());

            if (downloadSuccess)
            {
                try
                {
                    await AddToLidarrQueue(albumName, artistName).ConfigureAwait(false);
                    await NotifyLidarrForImport(downloadItem.AlbumDownloadPath).ConfigureAwait(false);
                    albumCache.MarkAlbumAsProcessed(artistName, albumName);
                    _stats.AddDownload(artistName, albumName, "Completed");
                }
                catch (Exception ex)
                {
                    // Don't change the status to Error here, just log it
                }
            }
        };

        try
        {
            await Task.Run(() =>
            {
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }).ConfigureAwait(false);

            return downloadSuccess;
        }
        catch (Exception)
        {
            await dispatcher.InvokeAsync(() => UpdateDownloadStatus(albumName, artistName, "Error"));
            return false;
        }
    }

    private string FormatPath(string format, string artistName, string albumName, string year)
    {
        return format
            .Replace("{Artist Name}", SafeFileName(artistName))
            .Replace("{Album Title}", SafeFileName(albumName))
            .Replace("{Release Year}", year);
    }

    private async Task<bool> IsLidarrAccessible()
    {
        if (!IsValidLidarrUrl(lidarrBaseUrl))
        {
            MessageBox.Show($"Invalid Lidarr URL. Please enter a valid URL like 'http://localhost:8686'.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
                var response = await client.GetAsync($"{LidarrApiUrl}/system/status").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Failed to connect to Lidarr. Please check your URL and API key.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                return true;
            }
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Failed to connect to Lidarr: {ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private string SafeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(System.IO.Path.GetInvalidFileNameChars()));
    }

    private async Task AddToLidarrQueue(string albumName, string artistName)
    {
        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var albumInfo = new
                {
                    title = albumName,
                    artistName = artistName
                };

                var content = new StringContent(JsonConvert.SerializeObject(albumInfo), Encoding.UTF8, "application/json");

                var response = await client.GetAsync($"{LidarrApiUrl}/album?artistName={Uri.EscapeDataString(artistName)}&albumName={Uri.EscapeDataString(albumName)}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    response = await client.PutAsync($"{LidarrApiUrl}/album", content).ConfigureAwait(false);
                }
                else
                {
                    response = await client.PostAsync($"{LidarrApiUrl}/album", content).ConfigureAwait(false);
                }

                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception)
        {
            // Log the exception
        }
    }

    private string RemoveAnsiEscapeSequences(string input)
    {
        string pattern = @"\x1B\[[^@-~]*[@-~]";
        return Regex.Replace(input, pattern, string.Empty);
    }

    private void UpdateDownloadBadge()
    {
        dispatcher.Invoke(() =>
        {
            mainWindow.DownloadBadgeCount.Text = activeDownloads.ToString();
            mainWindow.DownloadBadge.Visibility = activeDownloads > 0 ? Visibility.Visible : Visibility.Collapsed;
        });
    }

    private async Task<string> GetLidarrRootPath()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{LidarrApiUrl}/rootfolder").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var rootFolders = JArray.Parse(json);

            if (rootFolders.Count == 0)
                throw new Exception("No root folders configured in Lidarr");

            return rootFolders[0]["path"].ToString();
        }
    }

    public async Task RunDownloadProcess()
    {
        LogToFile("RunDownloadProcess started");

        if (!lidarrEnabled)
        {
            LogToFile("Lidarr is not enabled. Exiting RunDownloadProcess.");
            return;
        }

        if (!IsValidLidarrUrl(lidarrBaseUrl))
        {
            LogToFile($"Invalid Lidarr URL: {lidarrBaseUrl}");
            MessageBox.Show($"Invalid Lidarr URL. Please enter a valid URL like 'http://localhost:8686' in the settings.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!await IsLidarrAccessible().ConfigureAwait(false))
        {
            LogToFile("Lidarr is not accessible");
            return;
        }

        try
        {
            string lidarrRootPath = await GetLidarrRootPath().ConfigureAwait(false);
            LogToFile($"Lidarr root path: {lidarrRootPath}");

            List<JObject> wantedAlbums = await FetchWantedAlbumsFromLidarr().ConfigureAwait(false);

            foreach (var record in wantedAlbums)
            {
                string artistName = record["artist"]["artistName"].ToString();
                string albumName = record["title"].ToString();

                if (albumCache.IsAlbumProcessed(artistName, albumName))
                {
                    LogToFile($"Skipping already processed album: {albumName} by {artistName}");
                    continue;
                }

                try
                {
                    if (IsAlbumInDownloadQueue(artistName, albumName))
                    {
                        LogToFile($"Album already in download queue: {albumName} by {artistName}");
                        continue;
                    }

                    if (ShouldSkipAlbumDownload(artistName, albumName))
                    {
                        LogToFile($"Skipping album download: {albumName} by {artistName}");
                        continue;
                    }

                    var (albumExists, percentComplete) = await GetAlbumStatusInLidarr(artistName, albumName).ConfigureAwait(false);

                    if (albumExists && percentComplete == 100)
                    {
                        LogToFile($"Album already completed: {albumName} by {artistName}");
                        await UpdateDownloadStatus(albumName, artistName, "Completed");
                        continue;
                    }

                    string deezerId = await GetDeezerArtistID(artistName).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(deezerId))
                    {
                        string albumUrl = await QueryAlbumOnDeezer(deezerId, albumName, artistName).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(albumUrl))
                        {
                            bool downloadStarted = await StartDownloadFromDeezer(albumUrl, albumName, artistName, lidarrRootPath).ConfigureAwait(false);

                            if (downloadStarted)
                            {
                                LogToFile($"Download started for album: {albumName} by {artistName}");
                                await AddToLidarrQueue(albumName, artistName).ConfigureAwait(false);
                            }
                            else
                            {
                                LogToFile($"Download started for album: {albumName} by {artistName}");
                                await UpdateDownloadStatus(albumName, artistName, "Failed");
                            }
                        }
                        else
                        {
                            LogToFile($"Download started for album: {albumName} by {artistName}");
                            await UpdateDownloadStatus(albumName, artistName, "Not Found");
                        }
                    }
                    else
                    {
                        LogToFile($"Download started for album: {albumName} by {artistName}");
                        await UpdateDownloadStatus(albumName, artistName, "Artist Not Found");
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"Error processing album {albumName} by {artistName}: {ex.Message}");
                    await UpdateDownloadStatus(albumName, artistName, "Error");
                }
            }

            wantedAlbums.Clear(); // Clear the list once done
            ClearCompletedDownloads();
            LogToFile("RunDownloadProcess completed");
        }
        catch (Exception ex)
        {
            LogToFile($"Error in RunDownloadProcess: {ex.Message}");
        }
    }

    private void ClearCompletedDownloads()
    {
        dispatcher.Invoke(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            for (int i = downloadManagerControl.DownloadItems.Count - 1; i >= 0; i--)
            {
                if (downloadManagerControl.DownloadItems[i].Status == "Completed" || downloadManagerControl.DownloadItems[i].Status == "Failed" || downloadManagerControl.DownloadItems[i].Status == "Error")
                {
                    downloadManagerControl.DownloadItems.RemoveAt(i);
                }
            }
            stopwatch.Stop();
            //Debug.WriteLine($"Time to clear completed downloads: {stopwatch.ElapsedMilliseconds} ms");
        });
    }

    private async Task UpdateDownloadStatus(string albumName, string artistName, string status)
    {
        var downloadItem = downloadManagerControl.DownloadItems.FirstOrDefault(item =>
            item.Name == albumName && item.ArtistName == artistName);

        if (downloadItem != null)
        {
            await dispatcher.InvokeAsync(() =>
            {
                downloadItem.Status = status;
                downloadManagerControl.UpdateDownloadItemProgress(downloadItem, downloadItem.Progress, downloadItem.Progress);
            });
        }
    }

    public async Task NotifyLidarrForImport(string downloadPath)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);

            var command = new
            {
                name = "DownloadedAlbumsScan",
                path = downloadPath
            };

            var json = JsonConvert.SerializeObject(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync($"{LidarrApiUrl}/command", content);
                response.EnsureSuccessStatusCode();
                //Debug.WriteLine($"Successfully notified Lidarr to scan for imports in: {downloadPath}");
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error notifying Lidarr to scan for imports: {ex.Message}");
                // Consider re-throwing or handling this error
            }
        }
    }

    public List<DownloadInfo> GetActiveDownloads()
    {
        return downloadManagerControl.DownloadItems
            .Where(item => item.Status == "In progress" || item.Status == "Waiting")
            .Select(item => new DownloadInfo
            {
                ArtistName = item.ArtistName,
                AlbumName = item.Name,
                DownloadTime = item.StartTime,
                Status = item.Status
            }).ToList();
    }

    public async Task<(long totalSpace, long freeSpace)> GetLidarrDiskSpaceInfo()
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", lidarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync($"{lidarrBaseUrl}/api/v1/diskspace").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var diskSpaceData = JArray.Parse(json).FirstOrDefault();

            if (diskSpaceData != null)
            {
                long totalSpace = diskSpaceData.Value<long>("totalSpace");
                long freeSpace = diskSpaceData.Value<long>("freeSpace");
                return (totalSpace, freeSpace);
            }

            return (0, 0);
        }
    }

    public int GetDownloadProgress(string albumName, string artistName)
    {
        var downloadItem = downloadManagerControl.DownloadItems.FirstOrDefault(item =>
            item.Name == albumName && item.ArtistName == artistName);

        if (downloadItem == null)
        {
            return 0;
        }

        switch (downloadItem.Status)
        {
            case "Completed":
                return 100;
            case "Waiting":
                return 0;
            case "In progress":
                return downloadItem.Progress;
            case "Error":
            case "Failed":
                return 0;
            default:
                return 0;
        }
    }

    private async Task HandleDownloadCompletion(DownloadItem downloadItem)
    {
        downloadItem.EndTime = DateTime.Now;
        downloadItem.Status = "Completed";

        await dispatcher.InvokeAsync(() => {
            downloadManagerControl.UpdateDownloadItemProgress(downloadItem, downloadItem.Progress, 100);
        });

        db.UpdateDownloadItem(downloadItem);
        _stats.AddDownload(downloadItem.ArtistName, downloadItem.Name, "Completed");

        await NotifyLidarrForImport(downloadItem.AlbumDownloadPath);
    }

    //---------------------------------//DEEZER WEB UI DOWNLOAD CODE//------------------------------------------//

    public async Task<List<JObject>> SearchOnDeezer(string searchTerm, string searchType)
    {
        using (HttpClient client = new HttpClient())
        {
            var searchResults = new List<JObject>();

            for (int i = 0; i < 2; i++) // Iterate for 2 pages (0 and 1)
            {
                string baseUri = $"https://api.deezer.com/search/{searchType}/?q={Uri.EscapeDataString(searchTerm)}&index={i * 100}&limit=100&output=json";
                string json = await client.GetStringAsync(baseUri).ConfigureAwait(false);
                dynamic data = JsonConvert.DeserializeObject(json);

                if (data == null || data.data == null)
                {
                    throw new Exception("No data received from the Deezer API. Please check your search query or API endpoint.");
                }

                foreach (var item in data.data)
                {
                    var result = new JObject();

                    if (searchType == "artist")
                    {
                        result["artist"] = new JObject { ["name"] = item.name };
                        result["title"] = item.name;
                        result["link"] = item.link;
                        result["cover_medium"] = item.picture_medium;
                    }
                    else if (searchType == "track")
                    {
                        result["artist"] = new JObject { ["name"] = item.artist.name };
                        result["title"] = $"{item.title}";
                        result["link"] = item.link;
                        result["cover_medium"] = item.album.cover_medium;
                    }
                    else if (searchType == "album")
                    {
                        result["artist"] = new JObject { ["name"] = item.artist.name };
                        result["title"] = $"{item.title}";
                        result["link"] = item.link;
                        result["cover_medium"] = item.cover_medium;
                    }
                    else if (searchType == "playlist")
                    {
                        result["title"] = item.title;
                        result["link"] = item.link;
                        result["cover_medium"] = item.picture_medium;
                    }

                    searchResults.Add(result);
                }
            }

            return searchResults;
        }
    }

}
