using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Diagnostics;

namespace WpfApp1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
            EnsureDatabaseFile();
        }

        private void EnsureDatabaseFile()
        {
            var dbFileName = "downloads.db";
            var dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFileName);

            try
            {
                //Debug.WriteLine($"Database Path: {dbFilePath}");

                if (!File.Exists(dbFilePath))
                {
                    //Debug.WriteLine("Database file not found. Attempting to extract from resources.");
                    ExtractEmbeddedResource("BeatOn.downloads.db", dbFilePath);
                    //Debug.WriteLine("Successfully extracted database file.");
                }
                else
                {
                    //Debug.WriteLine("Database file already exists.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                //Debug.WriteLine($"Failed to copy database file: {ex.Message}");
                MessageBox.Show($"Failed to initialize the database. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
         
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                //Debug.WriteLine($"Unhandled exception: {exception.Message}");
            }
            else
            {
                //Debug.WriteLine("Unhandled exception: Unknown error");
            }
        }
    }
}
