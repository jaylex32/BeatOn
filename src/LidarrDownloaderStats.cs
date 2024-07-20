using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class LidarrDownloaderStats
    {
        private List<DownloadInfo> _recentDownloads = new List<DownloadInfo>();
        private LidarrDownloader _lidarrDownloader;

        public LidarrDownloaderStats()
        {
            // No parameters needed in the constructor
        }

        public void SetLidarrDownloader(LidarrDownloader lidarrDownloader)
        {
            _lidarrDownloader = lidarrDownloader;
        }

        public int GetDownloadCount()
        {
            return _recentDownloads.Count;
        }

        public List<DownloadInfo> GetRecentDownloads()
        {
            var completedDownloads = _recentDownloads
                .Where(d => d.Status == "Completed")
                .OrderByDescending(d => d.DownloadTime)
                .Take(10)
                .ToList();

            foreach (var download in completedDownloads)
            {
                //Console.WriteLine($"Download: {download.ArtistName} - {download.AlbumName}, Status: {download.Status}");
            }

            return completedDownloads;
        }

        public void AddDownload(string artistName, string albumName, string status)
        {
            _recentDownloads.Add(new DownloadInfo
            {
                ArtistName = artistName,
                AlbumName = albumName,
                DownloadTime = DateTime.Now,
                Status = status
            });

            if (_recentDownloads.Count > 100) // Keep only last 100 downloads
            {
                _recentDownloads.RemoveAt(0);
            }
        }
        public List<DownloadInfo> GetActiveDownloads()
        {
            if (_lidarrDownloader != null)
            {
                return _lidarrDownloader.GetActiveDownloads();
            }
            return new List<DownloadInfo>();
        }

        public List<DownloadInfo> GetFailedDownloads()
        {
            return _recentDownloads
                .Where(d => d.Status == "Failed" || d.Status == "Error")
                .OrderByDescending(d => d.DownloadTime)
                .Take(10)
                .ToList();
        }

        public async Task<StorageInfo> GetStorageInfo()
        {
            if (_lidarrDownloader != null)
            {
                var (totalSpace, freeSpace) = await _lidarrDownloader.GetLidarrDiskSpaceInfo();
                return new StorageInfo
                {
                    TotalSpace = totalSpace,
                    FreeSpace = freeSpace
                };
            }
            return null;
        }

    }

    public class DownloadInfo
    {
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public DateTime DownloadTime { get; set; }
        public string Status { get; set; }
    }

    public class StorageInfo
    {
        public long TotalSpace { get; set; }
        public long FreeSpace { get; set; }
    }
}
