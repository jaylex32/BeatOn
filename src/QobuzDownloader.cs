using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Windows;
using Newtonsoft.Json.Converters;
using System.Threading;

namespace WpfApp1
{
    public class QobuzDownloader
    {
        private readonly JObject settings;
        private readonly DatabaseManager db;
        private readonly DownloadManagerControl downloadManagerControl;
        private readonly Dispatcher dispatcher;
        private readonly LidarrDownloaderStats _stats;
        private readonly MainWindow mainWindow;
        private int activeDownloads;
        private bool captureNextLineAsAlbumPath;
        private readonly SemaphoreSlim downloadSemaphore;

        private JObject LoadSettingsFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new JObject();
            }
            string json = File.ReadAllText(filePath);
            return JObject.Parse(json);
        }

        private dynamic CreateDefaultSettings()
        {
            return new ExpandoObject();
        }

        public QobuzDownloader(DatabaseManager db, DownloadManagerControl downloadManagerControl, Dispatcher dispatcher, LidarrDownloaderStats stats, SemaphoreSlim semaphore, MainWindow mainWindow)
        {
            this.db = db;
            this.downloadManagerControl = downloadManagerControl;
            this.dispatcher = dispatcher;
            this._stats = stats;
            this.downloadSemaphore = semaphore;
            this.mainWindow = mainWindow;
            this.activeDownloads = 0;
            this.captureNextLineAsAlbumPath = false;
            string settingsFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "d-fi.config.json");
            this.settings = LoadSettingsFromFile(settingsFilePath);

            if (!ValidateQobuzSettings())
            {
                throw new InvalidOperationException("Invalid Qobuz settings. Please check your configuration.");
            }
        }

        private bool ValidateQobuzSettings()
        {
            return settings["qobuz"] != null &&
                   settings["qobuz"]["app_id"] != null &&
                   settings["qobuz"]["secrets"] != null;
        }

        public async Task<List<JObject>> SearchOnQobuz(string searchTerm, string searchType)
        {
            try
            {
                string baseUri = $"https://www.qobuz.com/api.json/0.2/{GetQobuzSearchApiPath(searchType)}{Uri.EscapeDataString(searchTerm)}&limit=50";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                    var queryParameters = new Dictionary<string, string>
                    {
                        { "app_secret", settings["qobuz"]?["secrets"]?.ToString() ?? string.Empty },
                        { "app_id", settings["qobuz"]?["app_id"]?.ToString() ?? string.Empty }
                    };

                    var content = new FormUrlEncodedContent(queryParameters);

                    var response = await client.PostAsync(baseUri, content);
                    response.EnsureSuccessStatusCode();

                    string json = await response.Content.ReadAsStringAsync();
                    JObject data = JObject.Parse(json);

                    return ProcessSearchResults(data, searchType);
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error in SearchOnQobuz: {ex}");
                throw;
            }
        }

        private List<JObject> ProcessSearchResults(JObject data, string searchType)
        {
            List<JObject> results = new List<JObject>();

            try
            {
                JArray itemsArray = data[searchType + "s"]?["items"] as JArray;
                if (itemsArray == null || !itemsArray.Any())
                {
                    //Debug.WriteLine($"No items found for search type: {searchType}");
                    return results;
                }

                foreach (JToken item in itemsArray)
                {
                    JObject result = new JObject();
                    try
                    {
                        string imageUrl = GetSafeImageUrl(item, searchType);

                        switch (searchType)
                        {
                            case "track":
                                result["artist"] = new JObject { ["name"] = item["performer"]?["name"]?.ToString() ?? "Unknown Artist" };
                                result["title"] = item["title"]?.ToString() ?? "Unknown Title";
                                result["link"] = $"http://open.qobuz.com/track/{item["id"]}";
                                result["cover_medium"] = imageUrl;
                                break;
                            case "album":
                                result["artist"] = new JObject { ["name"] = item["artist"]?["name"]?.ToString() ?? "Unknown Artist" };
                                result["title"] = item["title"]?.ToString() ?? "Unknown Album";
                                result["link"] = $"http://open.qobuz.com/album/{item["id"]}";
                                result["cover_medium"] = imageUrl;
                                break;
                            case "artist":
                                result["artist"] = new JObject { ["name"] = item["name"]?.ToString() ?? "Unknown Artist" };
                                result["title"] = item["name"]?.ToString() ?? "Unknown Artist";
                                result["link"] = $"http://open.qobuz.com/artist/{item["id"]}";
                                result["cover_medium"] = imageUrl;
                                break;
                            default:
                                //Debug.WriteLine($"Unsupported search type: {searchType}");
                                continue;
                        }
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        //Debug.WriteLine($"Error processing item for {searchType}: {ex.Message}");
                        // Continue to the next item instead of throwing an exception
                    }
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error in ProcessSearchResults: {ex.Message}");
                // Instead of throwing, we'll return the results we've managed to process
            }

            return results;
        }

        private string GetSafeImageUrl(JToken item, string searchType)
        {
            string defaultImageUrl = "path/to/default/image.jpg"; // Replace with your actual default image path

            JToken imageToken = searchType == "track" ? item["album"]?["image"] : item["image"];

            if (imageToken != null && imageToken.Type != JTokenType.Null)
            {
                JToken largeImageToken = imageToken["large"];
                if (largeImageToken != null && largeImageToken.Type != JTokenType.Null)
                {
                    return largeImageToken.ToString();
                }
            }

            return defaultImageUrl;
        }
        public async Task<bool> StartDownloadFromQobuzUsingSettingsPath(string albumUrl, string albumName, string artistName)
        {
            //Debug.WriteLine($"Starting Qobuz download for {albumName} by {artistName}");
            try
            {
                await downloadSemaphore.WaitAsync();
                activeDownloads++;
                UpdateDownloadBadge();

                DownloadItem downloadItem = new DownloadItem
                {
                    Name = albumName,
                    ArtistName = artistName,
                    Progress = 0,
                    StartTime = DateTime.Now,
                    Url = albumUrl,
                    Status = "Waiting",
                    SourceType = "Qobuz"
                };

                db.SaveDownloadItem(downloadItem);

                await dispatcher.InvokeAsync(() =>
                {
                    downloadManagerControl.AddDownloadItem(downloadItem);
                    _stats.AddDownload(artistName, albumName, "Waiting");
                });

                //Debug.WriteLine("Attempting to read QobuzQuality setting...");
                string qobuzQualityValue = MapQualityToValue(settings["QobuzQuality"]?.ToString());
                //Debug.WriteLine($"QobuzQuality: {qobuzQualityValue}");

                string qobuzFolderPath = settings["QobuzFolderPath"]?.ToString();
                //Debug.WriteLine($"QobuzFolderPath: {qobuzFolderPath}");

                string qobuzAlbumLayout = settings["saveLayout"]?["qobuz-album"]?.ToString();
                //Debug.WriteLine($"qobuzAlbumLayout: {qobuzAlbumLayout}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = @".\d-fi.exe",
                    Arguments = $@"/c -u '{albumUrl}' -q ""{qobuzQualityValue}"" -d -b -o ""{qobuzFolderPath}/{qobuzAlbumLayout}""",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                //Debug.WriteLine($"Download process arguments: {startInfo.Arguments}");

                bool downloadSuccess = false;

                using (Process process = new Process { StartInfo = startInfo, EnableRaisingEvents = true })
                {
                    downloadItem.DownloadProcess = process;
                    int totalTracks = 0;
                    int downloadedTracks = 0;

                    process.OutputDataReceived += async (s, output) =>
                    {
                        if (!string.IsNullOrEmpty(output.Data))
                        {
                            string outputMessage = output.Data;
                            //Debug.WriteLine($"Process output: {outputMessage}");

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

                                            downloadManagerControl.UpdateDownloadItemProgress(downloadItem, currentIndex, totalCount);

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
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            //Debug.WriteLine($"Process error: {e.Data}");
                            downloadItem.Output += "ERROR: " + e.Data + Environment.NewLine;
                        }
                    };

                    process.Exited += async (s, e) =>
                    {
                        try
                        {
                            downloadItem.EndTime = DateTime.Now;
                            int exitCode = process.ExitCode;
                            downloadSuccess = exitCode == 0;
                            string finalStatus = downloadSuccess ? "Completed" : "Failed";
                            await UpdateDownloadStatus(downloadItem, finalStatus);
                            //Debug.WriteLine($"Process exited with code: {exitCode}");
                            _stats.AddDownload(downloadItem.ArtistName, downloadItem.Name, finalStatus);
                        }
                        catch (InvalidOperationException ex)
                        {
                            //Debug.WriteLine($"Error accessing process properties: {ex.Message}");
                            await UpdateDownloadStatus(downloadItem, "Error");
                            _stats.AddDownload(downloadItem.ArtistName, downloadItem.Name, "Error");
                        }
                        finally
                        {
                            activeDownloads--;
                            UpdateDownloadBadge();
                            downloadSemaphore.Release();
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await Task.Run(() =>
                    {
                        try
                        {
                            process.WaitForExit();
                        }
                        catch (InvalidOperationException ex)
                        {
                            //Debug.WriteLine($"Error waiting for process exit: {ex.Message}");
                        }
                    });
                }

                return downloadSuccess;
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error in StartDownloadFromQobuzUsingSettingsPath: {ex}");
                await UpdateDownloadStatus(new DownloadItem { Name = albumName, ArtistName = artistName }, "Error");
                _stats.AddDownload(artistName, albumName, "Error");
                activeDownloads--;
                UpdateDownloadBadge();
                downloadSemaphore.Release();
                return false;
            }
        }

        private string GetSetting(dynamic settings, string path, string defaultValue)
        {
            try
            {
                //Debug.WriteLine($"Attempting to get setting: {path}");
                dynamic current = settings;
                foreach (var part in path.Split('.'))
                {
                    //Debug.WriteLine($"Accessing part: {part}");
                    current = current[part];
                }
                string result = current?.ToString() ?? defaultValue;
                //Debug.WriteLine($"Result for {path}: {result}");
                return result;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException ex)
            {
                //Debug.WriteLine($"Error accessing setting {path}: {ex.Message}");
                return defaultValue;
            }
        }

        private async Task UpdateDownloadStatus(DownloadItem downloadItem, string status)
        {
            await dispatcher.InvokeAsync(() =>
            {
                downloadItem.Status = status;
                int progress = status == "Completed" ? 100 : downloadItem.Progress;
                downloadManagerControl.UpdateDownloadItemProgress(downloadItem, progress, 100);
                db.UpdateDownloadItem(downloadItem);
            });
        }

        private async Task HandleDownloadCompletion(DownloadItem downloadItem)
        {
            downloadItem.EndTime = DateTime.Now;
            downloadItem.Status = "Completed";

            await dispatcher.InvokeAsync(() =>
            {
                downloadManagerControl.UpdateDownloadItemProgress(downloadItem, downloadItem.Progress, 100);
            });

            db.UpdateDownloadItem(downloadItem);
            _stats.AddDownload(downloadItem.ArtistName, downloadItem.Name, "Completed");
        }

        private string RemoveAnsiEscapeSequences(string input)
        {
            return Regex.Replace(input, @"\x1B\[[^@-~]*[@-~]", string.Empty);
        }

        private string GetQobuzSearchApiPath(string searchType)
        {
            switch (searchType)
            {
                case "track": return "track/search?query=";
                case "album": return "album/search?query=";
                case "artist": return "artist/search?query=";
                default: throw new ArgumentException("Invalid search type");
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

        private void UpdateDownloadBadge()
        {
            dispatcher.Invoke(() =>
            {
                mainWindow.DownloadBadgeCount.Text = activeDownloads.ToString();
                mainWindow.DownloadBadge.Visibility = activeDownloads > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
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

        // You might want to implement these methods if needed for Qobuz:
        // public async Task RunDownloadProcess() { ... }
        // public async Task<(long totalSpace, long freeSpace)> GetQobuzDiskSpaceInfo() { ... }
        // public async Task NotifyForImport(string downloadPath) { ... }

        // Add any additional methods specific to Qobuz functionality here
    }
}