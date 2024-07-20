using System;
using System.ComponentModel;
using System.Diagnostics;

public class DownloadItem : INotifyPropertyChanged
{
    public int Id { get; set; }

    // Property to store the path of the individual track
    public string DownloadPath { get; set; }

    // Property to store the path of the album
    public string AlbumDownloadPath { get; set; }

    public string SourceType { get; set; } // Add this property
    public Process DownloadProcess { get; set; }
    private string name;
    public string ArtistName { get; set; }
    private int progress;
    private string output;
    private string log;
    public bool IsDetailsVisible { get; set; } = false;
    public string Log
    {
        get { return log; }
        set
        {
            if (log != value)
            {
                log = value;
                OnPropertyChanged(nameof(Log));
            }
        }
    }
    private DateTime startTime;
    private DateTime? endTime;
    private string url;
    private string status;
    private string errorMessage;

    private string totalProgress;
    public string TotalProgress
    {
        get { return totalProgress; }
        set
        {
            if (totalProgress != value)
            {
                totalProgress = value;
                OnPropertyChanged(nameof(TotalProgress));
            }
        }
    }

    private double overallProgress;
    public double OverallProgress
    {
        get { return overallProgress; }
        set
        {
            if (overallProgress != value)
            {
                overallProgress = value;
                OnPropertyChanged(nameof(OverallProgress));
            }
        }
    }

    private double downloadProgress;
    public double DownloadProgress
    {
        get { return downloadProgress; }
        set
        {
            if (downloadProgress != value)
            {
                downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
                OnPropertyChanged(nameof(IsCompleted)); // Raise PropertyChanged event for IsCompleted when DownloadProgress changes
            }
        }
    }

    private string _rawOutput;

    public string RawOutput
    {
        get { return _rawOutput; }
        set
        {
            _rawOutput = value;
            OnPropertyChanged(nameof(RawOutput));
        }
    }

    private bool isCompleted;
    public bool IsCompleted
    {
        get { return isCompleted; }
        set
        {
            if (isCompleted != value)
            {
                isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(DownloadProgress)); // Raise PropertyChanged event for DownloadProgress when IsCompleted changes
            }
        }
    }

    public string Name
    {
        get { return name; }
        set
        {
            if (name != value)
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public int Progress
    {
        get { return progress; }
        set
        {
            if (progress != value)
            {
                progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }
    }

    public string Output
    {
        get { return output; }
        set
        {
            if (output != value)
            {
                output = value;
                OnPropertyChanged(nameof(Output));
            }
        }
    }

    public DateTime StartTime
    {
        get { return startTime; }
        set
        {
            if (startTime != value)
            {
                startTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }
    }

    public DateTime? EndTime
    {
        get { return endTime; }
        set
        {
            if (endTime != value)
            {
                endTime = value;
                OnPropertyChanged(nameof(EndTime));
            }
        }
    }

    public string Url
    {
        get { return url; }
        set
        {
            if (url != value)
            {
                url = value;
                OnPropertyChanged(nameof(Url));
            }
        }
    }

    public string Status
    {
        get { return status; }
        set
        {
            if (status != value)
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    public string ErrorMessage
    {
        get { return errorMessage; }
        set
        {
            if (errorMessage != value)
            {
                errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}