using System;
using System.IO;

public class DownloadTaskInfo
{
    public string Url { get; set; }
    public string SavePath { get; set; }
    public int ThreadCount { get; set; } = 4;
    public long TotalSize { get; set; }
    public long DownloadedSize { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ErrorMessage { get; set; }
}

public enum DownloadStatus
{
    Pending,
    Downloading,
    Paused,
    Completed,
    Failed,
    Cancelled
}