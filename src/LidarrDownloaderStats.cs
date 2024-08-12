using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public void UpdateDownloadProgress(string artistName, string albumName, int progress)
        {
            var download = _recentDownloads.FirstOrDefault(d => d.ArtistName == artistName && d.AlbumName == albumName);
            if (download != null)
            {
                download.Progress = progress;
                if (progress == 100)
                {
                    download.Status = "Completed";
                }
            }
        }

        public void AddDownload(string artistName, string albumName, string status)
        {
            var existingDownload = _recentDownloads.FirstOrDefault(d => d.ArtistName == artistName && d.AlbumName == albumName);
            if (existingDownload != null)
            {
                existingDownload.Status = status;
                existingDownload.DownloadTime = DateTime.Now;
            }
            else
            {
                _recentDownloads.Add(new DownloadInfo
                {
                    ArtistName = artistName,
                    AlbumName = albumName,
                    DownloadTime = DateTime.Now,
                    Status = status
                });
            }

            if (_recentDownloads.Count > 100) // Keep only last 100 downloads
            {
                _recentDownloads.RemoveAt(0);
            }
        }

        public List<DownloadInfo> GetActiveDownloads()
        {
            return _recentDownloads.Where(d => d.Status == "In progress" || d.Status == "Waiting" || d.Status == "Completed").ToList();
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
            DriveInfo drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory));
            return new StorageInfo
            {
                TotalSpace = drive.TotalSize,
                FreeSpace = drive.AvailableFreeSpace
            };
        }

        public List<DownloadInfo> GetRecentCompletedDownloads()
        {
            return _recentDownloads
                .Where(d => d.Status == "Completed")
                .OrderByDescending(d => d.DownloadTime)
                .Take(10)
                .ToList();
        }
    }



    public class DownloadInfo
    {
        public string ArtistName { get; set; }
        public string AlbumName { get; set; }
        public DateTime DownloadTime { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
    }

    public class StorageInfo
    {
        public long TotalSpace { get; set; }
        public long FreeSpace { get; set; }
    }
}
