using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class DatabaseManager
{
    private string connectionString;
    private List<DownloadItem> downloadItems;

    public DatabaseManager()
    {
        var dbFileName = "downloads.db";
        var dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);
        connectionString = $"Data Source={dbFilePath};Version=3;";
        downloadItems = new List<DownloadItem>();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                CREATE TABLE IF NOT EXISTS Downloads (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    StartTime TEXT NOT NULL,
                    EndTime TEXT,
                    Url TEXT NOT NULL,
                    Output TEXT,
                    Status TEXT NOT NULL,
                    ErrorMessage TEXT,
                    Log TEXT 
                )";

                using (SQLiteCommand command = new SQLiteCommand(sql, conn))
                {
                    command.ExecuteNonQuery();
                }

                //Debug.WriteLine("Database initialized successfully.");
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            //Debug.WriteLine($"Database initialization failed: {ex.Message}");
        }
    }

    public void SaveDownloadItem(DownloadItem downloadItem)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            string sql = @"
            INSERT INTO Downloads (StartTime, EndTime, Url, Output, Status, ErrorMessage)
            VALUES (@StartTime, @EndTime, @Url, @Output, @Status, @ErrorMessage)";

            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                command.Parameters.AddWithValue("@StartTime", downloadItem.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@EndTime", downloadItem.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Url", downloadItem.Url);
                command.Parameters.AddWithValue("@Output", downloadItem.Output);
                command.Parameters.AddWithValue("@Status", downloadItem.Status);
                command.Parameters.AddWithValue("@ErrorMessage", downloadItem.ErrorMessage);

                command.ExecuteNonQuery();
            }
        }
    }

    // Add this method to get a download item based on artist and album name
    public DownloadItem GetDownloadItem(string artistName, string albumName)
    {
        return downloadItems.FirstOrDefault(item => item.ArtistName == artistName && item.Name == albumName);
    }

    public void UpdateDownloadItem(DownloadItem downloadItem)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            string sql = @"
            UPDATE Downloads
            SET EndTime = @EndTime, Output = @Output, Status = @Status, ErrorMessage = @ErrorMessage
            WHERE Id = @Id";

            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                command.Parameters.AddWithValue("@EndTime", downloadItem.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Output", downloadItem.Output);
                command.Parameters.AddWithValue("@Status", downloadItem.Status);
                command.Parameters.AddWithValue("@ErrorMessage", downloadItem.ErrorMessage);
                command.Parameters.AddWithValue("@Id", downloadItem.Id);

                command.ExecuteNonQuery();
            }
        }
    }

    public DownloadItem GetDownloadItem(int id)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            string sql = "SELECT * FROM Downloads WHERE Id = @Id";

            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                command.Parameters.AddWithValue("@Id", id);

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new DownloadItem
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            StartTime = Convert.ToDateTime(reader["StartTime"]),
                            EndTime = reader["EndTime"] != DBNull.Value ? Convert.ToDateTime(reader["EndTime"]) : (DateTime?)null,
                            Url = Convert.ToString(reader["Url"]),
                            Output = Convert.ToString(reader["Output"]),
                            Status = Convert.ToString(reader["Status"]),
                            ErrorMessage = Convert.ToString(reader["ErrorMessage"]),
                            Log = Convert.ToString(reader["Log"])
                        };
                    }
                }
            }
        }

        return null; // Return null if the download item with the specified id is not found
    }

    public void UpdateDownloadItemLog(DownloadItem downloadItem)
    {
        using (SQLiteConnection conn = new SQLiteConnection(connectionString))
        {
            conn.Open();

            string sql = @"
            UPDATE Downloads
            SET Log = @Log
            WHERE Id = @Id";

            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                command.Parameters.AddWithValue("@Log", downloadItem.Log);
                command.Parameters.AddWithValue("@Id", downloadItem.Id);

                command.ExecuteNonQuery();
            }
        }
    }
}
