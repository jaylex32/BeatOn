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

namespace WpfApp1
{
    public class WebUIServer
    {
        private readonly LidarrDownloaderStats _stats;
        private readonly LidarrDownloader _lidarrDownloader;
        private readonly DatabaseManager _db;
        private HttpListener _listener;
        private readonly string _url;

        public WebUIServer(LidarrDownloaderStats stats, LidarrDownloader lidarrDownloader, DatabaseManager db, string url)
        {
            _stats = stats;
            _lidarrDownloader = lidarrDownloader;
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
            string filename = context.Request.Url.AbsolutePath;

            if (filename == "/test")
            {
                await HandleTestRequest(context);
            }
            else if (filename == "/api/search")
            {
                await HandleSearchRequest(context);
            }
            else if (filename == "/api/download")
            {
                await HandleDownloadRequest(context);
            }
            else if (filename == "/api/status")
            {
                await HandleApiRequest(context);
            }
            else if (filename == "/api/system")
            {
                await HandleSystemRequest(context);
            }
            else if (filename.StartsWith("/api/download-status"))
            {
                await HandleDownloadStatusRequest(context);
            }
            else
            {
                await HandleEmbeddedFileRequest(context, filename);
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
            var storageInfo = await _stats.GetStorageInfo();
            var statusData = new
            {
                Status = "Running",
                DownloadCount = _stats.GetDownloadCount(),
                RecentDownloads = _stats.GetRecentDownloads(),
                ActiveDownloads = _stats.GetActiveDownloads(),
                FailedDownloads = _stats.GetFailedDownloads(),
                StorageInfo = storageInfo
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
                    var searchRequest = JsonConvert.DeserializeObject<SearchRequest>(requestBody);

                    var searchResults = await _lidarrDownloader.SearchOnDeezer(searchRequest.SearchTerm, searchRequest.SearchType).ConfigureAwait(false);

                    var results = searchResults.Select(result => new
                    {
                        artistName = result["artist"]?["name"]?.ToString() ?? "Unknown Artist",
                        title = result["title"]?.ToString() ?? "Unknown Title",
                        link = result["link"]?.ToString() ?? "No Link",
                        cover = result["cover_medium"]?.ToString() // Ensure cover image is included
                    }).ToList();

                    var jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(results);
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.ContentType = "application/json";
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/plain";
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private async Task HandleDownloadRequest(HttpListenerContext context)
        {
            try
            {
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    string requestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var downloadRequest = JsonConvert.DeserializeObject<DownloadRequest>(requestBody);

                    bool success = await _lidarrDownloader.StartDownloadFromDeezerUsingSettingsPath(downloadRequest.AlbumUrl, downloadRequest.AlbumName, downloadRequest.ArtistName).ConfigureAwait(false);

                    var response = new { Success = success };
                    var jsonResponse = Newtonsoft.Json.JsonConvert.SerializeObject(response);
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.ContentType = "application/json";
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                byte[] buffer = Encoding.UTF8.GetBytes(ex.Message);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/plain";
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
            finally
            {
                context.Response.OutputStream.Close();
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
                Status = downloadItem?.Status ?? "Unknown"
            };

            string jsonResponse = System.Text.Json.JsonSerializer.Serialize(statusData);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        private async Task HandleEmbeddedFileRequest(HttpListenerContext context, string filename)
        {
            var response = context.Response;
            string resourcePath = $"WpfApp1.wwwroot{filename.Replace('/', '.')}";

            if (filename == "/")
            {
                resourcePath = "WpfApp1.wwwroot.index.html";
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
            var response = context.Response;
            var systemStats = new SystemUsageStats();
            var systemData = new
            {
                CpuUsage = systemStats.GetCpuUsage(),
                MemoryUsage = systemStats.GetPrivateMemorySize(),
                TotalMemory = systemStats.GetTotalMemory()
            };

            string jsonResponse = System.Text.Json.JsonSerializer.Serialize(systemData);
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
        }

        public class DownloadRequest
        {
            public string AlbumUrl { get; set; }
            public string AlbumName { get; set; }
            public string ArtistName { get; set; }
            public string LidarrRootPath { get; set; }
        }
    }
}
