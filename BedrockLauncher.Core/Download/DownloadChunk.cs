public class DownloadChunk
{
    public int Index { get; set; }
    public long StartPosition { get; set; }
    public long EndPosition { get; set; }
    public long DownloadedSize { get; set; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Pending;
    public string TempFilePath { get; set; }
}