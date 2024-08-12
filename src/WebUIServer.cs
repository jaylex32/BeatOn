using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace WpfApp1
{
    public class WebUIServer
    {
        private readonly LidarrDownloaderStats _stats;
        private readonly LidarrDownloader _lidarrDownloader;
        private readonly QobuzDownloader _qobuzDownloader;
        private readonly DatabaseManager _db;
        private HttpListener _listener;
        private readonly string _url;

        public WebUIServer(LidarrDownloaderStats stats, LidarrDownloader lidarrDownloader, QobuzDownloader qobuzDownloader, DatabaseManager db, string url)
        {
            _stats = stats;
            _lidarrDownloader = lidarrDownloader;
            _qobuzDownloader = qobuzDownloader ?? throw new ArgumentNullException(nameof(qobuzDownloader));
            _db = db;
            _url = url.Replace("localhost", "+");
        }

        public async Task StartAsync()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            while (true)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
        }

        public void Stop()
        {
            _listener?.Stop();
            _listener = null;
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath;

            if (path == "/test")
            {
                await HandleTestRequest(context);
            }
            else if (path == "/api/search")
            {
                await HandleSearchRequest(context);
            }
            else if (path.StartsWith("/api/download"))
            {
                await HandleDownloadOperations(context);
            }
            else if (path == "/api/status")
            {
                await HandleApiRequest(context);
            }
            else if (path == "/api/system")
            {
                await HandleSystemRequest(context);
            }
            else
            {
                await HandleEmbeddedFileRequest(context, path);
            }
        }

        private async Task HandleTestRequest(HttpListenerContext context)
        {
            var response = context.Response;
            string responseString = "<HTML><BODY>Test successful</BODY></HTML>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        private async Task HandleApiRequest(HttpListenerContext context)
        {
            var response = context.Response;
            var statusData = new
            {
                Status = "Running",
                DownloadCount = _stats.GetDownloadCount(),
                RecentDownloads = _stats.GetRecentDownloads(),
                ActiveDownloads = _stats.GetActiveDownloads(),
                FailedDownloads = _stats.GetFailedDownloads(),
                StorageInfo = await _stats.GetStorageInfo()
            };

            string jsonResponse = System.Text.Json.JsonSerializer.Serialize(statusData);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        private async Task HandleSearchRequest(HttpListenerContext context)
        {
            try
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                    //Debug.WriteLine($"Received search request: {requestBody}");

                    var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(requestBody);

                    if (searchRequest == null)
                    {
                        throw new ArgumentNullException(nameof(searchRequest), "Search request could not be deserialized.");
                    }

                    //Debug.WriteLine($"Searching for {searchRequest.SearchTerm} in {searchRequest.SearchType} using {searchRequest.Service}");

                    List<JObject> searchResults;
                    if (searchRequest.Service == "qobuz")
                    {
                        if (_qobuzDownloader == null)
                        {
                            throw new InvalidOperationException("QobuzDownloader is not initialized.");
                        }
                        searchResults = await _qobuzDownloader.SearchOnQobuz(searchRequest.SearchTerm, searchRequest.SearchType).ConfigureAwait(false);
                    }
                    else
                    {
                        if (_lidarrDownloader == null)
                        {
                            throw new InvalidOperationException("LidarrDownloader is not initialized.");
                        }
                        searchResults = await _lidarrDownloader.SearchOnDeezer(searchRequest.SearchTerm, searchRequest.SearchType).ConfigureAwait(false);
                    }

                    if (searchResults == null)
                    {
                        throw new InvalidOperationException("Search results are null.");
                    }

                    //Debug.WriteLine($"Search completed. Found {searchResults.Count} results.");

                    var results = searchResults.Select(result => new
                    {
                        artistName = result["artist"]?["name"]?.ToString() ?? "Unknown Artist",
                        title = result["title"]?.ToString() ?? "Unknown Title",
                        link = result["link"]?.ToString() ?? "No Link",
                        cover = result["cover_medium"]?.ToString()
                    }).ToList();

                    await SendJsonResponse(context.Response, new { Success = true, Results = results });
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error in HandleSearchRequest: {ex}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await SendJsonResponse(context.Response, new { Success = false, Error = ex.Message, Stack = ex.StackTrace });
            }
        }
        private async Task HandleDownloadOperations(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST")
            {
                await HandleDownloadRequest(context);
            }
            else if (request.HttpMethod == "GET")
            {
                string path = request.Url.AbsolutePath;
                if (path.EndsWith("/download-progress"))
                {
                    await HandleDownloadProgressRequest(context);
                }
                else if (path.EndsWith("/download-status"))
                {
                    await HandleDownloadStatusRequest(context);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await SendJsonResponse(response, new { Error = "Invalid operation" });
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                await SendJsonResponse(response, new { Error = "Method not allowed" });
            }
        }

        private async Task HandleDownloadRequest(HttpListenerContext context)
        {
            try
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                    //Debug.WriteLine($"Received download request: {requestBody}");

                    var downloadRequest = JsonConvert.DeserializeObject<DownloadRequest>(requestBody);

                    //Debug.WriteLine($"Attempting to download {downloadRequest.AlbumName} by {downloadRequest.ArtistName} from {downloadRequest.Service}");

                    bool success;
                    if (downloadRequest.Service == "qobuz")
                    {
                        success = await _qobuzDownloader.StartDownloadFromQobuzUsingSettingsPath(
                            downloadRequest.AlbumUrl,
                            downloadRequest.AlbumName,
                            downloadRequest.ArtistName
                        ).ConfigureAwait(false);
                    }
                    else
                    {
                        success = await _lidarrDownloader.StartDownloadFromDeezerUsingSettingsPath(
                            downloadRequest.AlbumUrl,
                            downloadRequest.AlbumName,
                            downloadRequest.ArtistName
                        ).ConfigureAwait(false);
                    }

                    //Debug.WriteLine($"Download success: {success}");

                    await SendJsonResponse(context.Response, new { Success = success });
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Error in HandleDownloadRequest: {ex}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await SendJsonResponse(context.Response, new { Error = ex.Message, Stack = ex.StackTrace });
            }
        }

        private async Task HandleDownloadStatusRequest(HttpListenerContext context)
        {
            var query = context.Request.QueryString;
            string albumName = query["albumName"];
            string artistName = query["artistName"];

            var downloadItem = _db.GetDownloadItem(albumName, artistName);
            var statusData = new
            {
                Status = downloadItem?.Status ?? "Unknown",
                Progress = GetDownloadProgress(new DownloadInfo { AlbumName = albumName, ArtistName = artistName })
            };

            await SendJsonResponse(context.Response, statusData);
        }

        private async Task HandleDownloadProgressRequest(HttpListenerContext context)
        {
            var response = context.Response;
            var activeDownloads = _lidarrDownloader.GetActiveDownloads();
            var progressData = activeDownloads.Select(download => new
            {
                artistName = download.ArtistName,
                albumName = download.AlbumName,
                status = download.Status.ToLower(),
                progress = download.Progress
            }).ToList();

            string jsonResponse = System.Text.Json.JsonSerializer.Serialize(progressData);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        private int GetDownloadProgress(DownloadInfo download)
        {
            var downloadItem = _db.GetDownloadItem(download.AlbumName, download.ArtistName);
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
                    // Estimate progress based on time elapsed
                    var elapsedTime = (DateTime.Now - downloadItem.StartTime).TotalSeconds;
                    var estimatedTotalTime = 300; // Assume 5 minutes for a download
                    var estimatedProgress = (int)Math.Min(99, (elapsedTime / estimatedTotalTime) * 100);
                    return estimatedProgress;
                case "Error":
                case "Failed":
                    return 0;
                default:
                    return 0;
            }
        }

        private async Task HandleEmbeddedFileRequest(HttpListenerContext context, string filename)
        {
            var response = context.Response;
            string resourcePath;

            if (filename == "/")
            {
                resourcePath = "BeatOn.wwwroot.search.html";
            }
            else if (filename == "/search")
            {
                resourcePath = "BeatOn.wwwroot.search.html";
            }
            else if (filename == "/downloads")
            {
                resourcePath = "BeatOn.wwwroot.index.html";
            }
            else
            {
                // Remove leading slash and replace remaining slashes with dots
                resourcePath = $"BeatOn.wwwroot{filename.TrimStart('/').Replace('/', '.')}";

                // If no file extension is provided, assume .html
                if (!Path.HasExtension(resourcePath))
                {
                    resourcePath += ".html";
                }
            }

            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
            {
                if (resourceStream == null)
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                    return;
                }

                byte[] buffer = new byte[resourceStream.Length];
                await resourceStream.ReadAsync(buffer, 0, buffer.Length);

                response.ContentLength64 = buffer.Length;
                response.ContentType = GetContentType(resourcePath);
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();
            }
        }

        private async Task HandleSystemRequest(HttpListenerContext context)
        {
            var systemStats = new SystemUsageStats();
            var systemData = new
            {
                CpuUsage = systemStats.GetCpuUsage(),
                MemoryUsage = systemStats.GetPrivateMemorySize(),
                TotalMemory = systemStats.GetTotalMemory()
            };

            await SendJsonResponse(context.Response, systemData);
        }

        private async Task SendJsonResponse(HttpListenerResponse response, object data)
        {
            string jsonResponse = System.Text.Json.JsonSerializer.Serialize(data);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        private string GetContentType(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                _ => "application/octet-stream",
            };
        }

        public class SearchRequest
        {
            public string SearchTerm { get; set; }
            public string SearchType { get; set; }
            public string Service { get; set; }
        }

        public class DownloadRequest
        {
            public string AlbumUrl { get; set; }
            public string AlbumName { get; set; }
            public string ArtistName { get; set; }
            public string LidarrRootPath { get; set; }
            public string Service { get; set; }
        }
    }
}