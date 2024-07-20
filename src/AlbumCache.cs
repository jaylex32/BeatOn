using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class AlbumCache
    {
        private Dictionary<string, bool> cache = new Dictionary<string, bool>();
        private readonly TimeSpan cacheDuration = TimeSpan.FromHours(24); // Cache for 24 hours
        private readonly string cacheFilePath;

        public AlbumCache(string cacheFilePath)
        {
            this.cacheFilePath = cacheFilePath;
            LoadCache();
        }

        public bool IsAlbumProcessed(string artistName, string albumName)
        {
            string key = GetCacheKey(artistName, albumName);
            return cache.ContainsKey(key);
        }

        public void MarkAlbumAsProcessed(string artistName, string albumName)
        {
            string key = GetCacheKey(artistName, albumName);
            cache[key] = true;
            SaveCache();
        }

        private string GetCacheKey(string artistName, string albumName)
        {
            return $"{artistName}|{albumName}".ToLower();
        }

        private void LoadCache()
        {
            if (File.Exists(cacheFilePath))
            {
                var lines = File.ReadAllLines(cacheFilePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3 && DateTime.TryParse(parts[2], out DateTime timestamp))
                    {
                        if (DateTime.Now - timestamp <= cacheDuration)
                        {
                            cache[parts[0] + "|" + parts[1]] = true;
                        }
                    }
                }
            }
        }

        private void SaveCache()
        {
            var lines = cache.Select(kvp => $"{kvp.Key}|{DateTime.Now:O}");
            File.WriteAllLines(cacheFilePath, lines);
        }
    }
}
