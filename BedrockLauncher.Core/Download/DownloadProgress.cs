using System;

public class DownloadProgressEventArgs : EventArgs
{
    public string Url { get; set; }
    public long TotalSize { get; set; }
    public long DownloadedSize { get; set; }
    public double ProgressPercentage { get; set; }
    public double Speed { get; set; } // bytes per second
    public TimeSpan RemainingTime { get; set; }
}

public class DownloadCompletedEventArgs : EventArgs
{
    public string Url { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}