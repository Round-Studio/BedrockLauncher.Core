public class MultiThreadDownloader : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly List<DownloadChunk> _chunks;
    private readonly object _progressLock = new object();
    private readonly CancellationTokenSource _cancellationTokenSource;

    private DownloadTaskInfo _taskInfo;
    private long _lastProgressReport;
    private DateTime _lastProgressTime;
    private bool _disposed = false;
    private Task _progressTask;
    private volatile bool _isDownloadCompleted = false;
    private bool _supportsRangeRequests = true;

    public event EventHandler<DownloadProgressEventArgs> ProgressChanged;
    public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;

    public MultiThreadDownloader()
    {
        _httpClient = new HttpClient(new HttpClientHandler()
        {
            AllowAutoRedirect = true,
            UseCookies = false
        });

        _httpClient.Timeout = TimeSpan.FromMinutes(30);
        _chunks = new List<DownloadChunk>();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task<bool> DownloadAsync(string url, string savePath, int threadCount = 4)
    {
        try
        {
            _taskInfo = new DownloadTaskInfo
            {
                Url = url,
                SavePath = savePath,
                ThreadCount = threadCount,
                StartTime = DateTime.Now,
                Status = DownloadStatus.Downloading
            };

            // 检查服务器是否支持断点续传和获取文件大小
            await CheckServerCapabilities(url);

            if (_taskInfo.TotalSize > 0 && _supportsRangeRequests)
            {
                // 支持多线程下载
                await StartMultiThreadDownload();
            }
            else
            {
                // 不支持多线程，使用单线程下载
                await StartSingleThreadDownload();
            }

            return true;
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
        {
            _taskInfo.Status = DownloadStatus.Cancelled;
            OnDownloadCompleted(new DownloadCompletedEventArgs
            {
                Url = _taskInfo?.Url ?? url,
                Success = false,
                ErrorMessage = "下载被取消"
            });
            return false;
        }
        catch (Exception ex)
        {
            _taskInfo.Status = DownloadStatus.Failed;
            _taskInfo.ErrorMessage = ex.Message;
            OnDownloadCompleted(new DownloadCompletedEventArgs
            {
                Url = _taskInfo?.Url ?? url,
                Success = false,
                ErrorMessage = ex.Message
            });
            return false;
        }
    }

    private async Task CheckServerCapabilities(string url)
    {
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead);

            if (headResponse.IsSuccessStatusCode)
            {
                // 检查是否支持断点续传
                var acceptRanges = headResponse.Headers.FirstOrDefault(h => h.Key.Equals("Accept-Ranges", StringComparison.OrdinalIgnoreCase));
                _supportsRangeRequests = acceptRanges.Value?.FirstOrDefault()?.Equals("bytes", StringComparison.OrdinalIgnoreCase) == true;

                // 获取文件大小
                _taskInfo.TotalSize = headResponse.Content.Headers.ContentLength ?? 0;
            }
            else
            {
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
                getRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

                var rangeResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead);

                if (rangeResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
                {
                    _supportsRangeRequests = true;
                    _taskInfo.TotalSize = rangeResponse.Content.Headers.ContentLength ?? 0;
                }
                else
                {
                    _supportsRangeRequests = false;
                    _taskInfo.TotalSize = 0;
                }
            }
        }
        catch
        {
            // 如果检查失败，默认不支持多线程
            _supportsRangeRequests = false;
            _taskInfo.TotalSize = 0;
        }
    }

    private async Task StartMultiThreadDownload()
    {
        // 创建下载分片
        CreateChunks(_taskInfo.ThreadCount);

        // 检查临时文件并恢复下载
        await ResumeDownloadIfPossible();

        // 开始多线程下载
        await StartDownload();
    }

    private async Task StartSingleThreadDownload()
    {
        // 单线程下载，创建一个完整的分片
        _chunks.Clear();
        var chunk = new DownloadChunk
        {
            Index = 0,
            StartPosition = 0,
            EndPosition = long.MaxValue, // 未知大小
            TempFilePath = $"{_taskInfo.SavePath}.part0"
        };
        _chunks.Add(chunk);

        // 检查临时文件并恢复下载
        if (File.Exists(chunk.TempFilePath))
        {
            var fileInfo = new FileInfo(chunk.TempFilePath);
            chunk.DownloadedSize = fileInfo.Length;
        }

        // 启动进度报告任务
        _progressTask = Task.Run(() => ReportProgressAsync());

        try
        {
            // 执行单线程下载
            await DownloadSingleThreadChunkAsync(chunk);

            // 合并文件（实际上是重命名）
            await MergeSingleFile();

            _taskInfo.Status = DownloadStatus.Completed;
            _taskInfo.EndTime = DateTime.Now;

            _isDownloadCompleted = true;
            await StopProgressReporting();

            OnDownloadCompleted(new DownloadCompletedEventArgs
            {
                Url = _taskInfo.Url,
                Success = true
            });
        }
        catch (Exception)
        {
            _isDownloadCompleted = true;
            await StopProgressReporting();
            throw;
        }
    }

    private void CreateChunks(int threadCount)
    {
        _chunks.Clear();
        if (_taskInfo.TotalSize <= 0) return;

        long chunkSize = _taskInfo.TotalSize / threadCount;

        for (int i = 0; i < threadCount; i++)
        {
            var chunk = new DownloadChunk
            {
                Index = i,
                StartPosition = i * chunkSize,
                EndPosition = (i == threadCount - 1) ? _taskInfo.TotalSize - 1 : ((i + 1) * chunkSize) - 1,
                TempFilePath = $"{_taskInfo.SavePath}.part{i}"
            };
            _chunks.Add(chunk);
        }
    }

    private async Task ResumeDownloadIfPossible()
    {
        foreach (var chunk in _chunks)
        {
            if (File.Exists(chunk.TempFilePath))
            {
                var fileInfo = new FileInfo(chunk.TempFilePath);
                chunk.DownloadedSize = fileInfo.Length;

                if (chunk.DownloadedSize == (chunk.EndPosition - chunk.StartPosition + 1))
                {
                    chunk.Status = DownloadStatus.Completed;
                }
                else
                {
                    chunk.Status = DownloadStatus.Pending;
                }
            }
        }
    }

    private async Task StartDownload()
    {
        var downloadTasks = new List<Task>();

        foreach (var chunk in _chunks.Where(c => c.Status != DownloadStatus.Completed))
        {
            var chunkCopy = chunk; // 避免闭包问题
            downloadTasks.Add(DownloadChunkAsync(chunkCopy));
        }

        // 启动进度报告任务
        _progressTask = Task.Run(() => ReportProgressAsync());

        try
        {
            // 等待所有下载任务完成
            await Task.WhenAll(downloadTasks);

            // 标记下载完成，停止进度报告
            _isDownloadCompleted = true;

            // 等待所有分片完成后再合并文件
            await MergeChunks();

            _taskInfo.Status = DownloadStatus.Completed;
            _taskInfo.EndTime = DateTime.Now;

            // 停止进度报告
            await StopProgressReporting();

            OnDownloadCompleted(new DownloadCompletedEventArgs
            {
                Url = _taskInfo.Url,
                Success = true
            });
        }
        catch (Exception)
        {
            _isDownloadCompleted = true;
            await StopProgressReporting();
            throw;
        }
    }

    private async Task StopProgressReporting()
    {
        try
        {
            // 给进度报告任务一些时间优雅退出
            int retryCount = 0;
            while (!_isDownloadCompleted && retryCount < 5)
            {
                await Task.Delay(100);
                retryCount++;
            }

            // 如果进度任务还在运行，尝试等待
            if (_progressTask != null && !_progressTask.IsCompleted)
            {
                await Task.WhenAny(_progressTask, Task.Delay(1000));
            }
        }
        catch
        {
            // 忽略停止进度报告时的异常
        }
    }

    private async Task DownloadChunkAsync(DownloadChunk chunk)
    {
        // 检查是否已经被取消
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            chunk.Status = DownloadStatus.Cancelled;
            return;
        }

        try
        {
            using var fileStream = new FileStream(
                chunk.TempFilePath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite);

            fileStream.Seek(chunk.DownloadedSize, SeekOrigin.Begin);

            using var request = new HttpRequestMessage(HttpMethod.Get, _taskInfo.Url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(
                chunk.StartPosition + chunk.DownloadedSize,
                chunk.EndPosition);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                _cancellationTokenSource.Token);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.PartialContent)
            {
                throw new Exception($"下载分片失败: {response.StatusCode}");
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                chunk.DownloadedSize += bytesRead;

                _taskInfo.DownloadedSize += bytesRead;

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }

            chunk.Status = DownloadStatus.Completed;
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
        {
            chunk.Status = DownloadStatus.Cancelled;
            // 这是预期的行为，不抛出异常
        }
        catch (Exception ex)
        {
            chunk.Status = DownloadStatus.Failed;
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                throw new Exception($"下载分片 {chunk.Index} 失败: {ex.Message}", ex);
            }
        }
    }

    private async Task DownloadSingleThreadChunkAsync(DownloadChunk chunk)
    {
        try
        {
            using var fileStream = new FileStream(
                chunk.TempFilePath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite);

            fileStream.Seek(chunk.DownloadedSize, SeekOrigin.Begin);

            using var request = new HttpRequestMessage(HttpMethod.Get, _taskInfo.Url);

            // 如果已经下载了一部分，设置 Range 头部
            if (chunk.DownloadedSize > 0 && _supportsRangeRequests)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(chunk.DownloadedSize, null);
            }

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                _cancellationTokenSource.Token);

            if (!response.IsSuccessStatusCode &&
                !(response.StatusCode == System.Net.HttpStatusCode.PartialContent && _supportsRangeRequests))
            {
                throw new Exception($"下载失败: {response.StatusCode}");
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(_cancellationTokenSource.Token);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, _cancellationTokenSource.Token);
                chunk.DownloadedSize += bytesRead;

                _taskInfo.DownloadedSize += bytesRead;

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    break;
            }

            chunk.Status = DownloadStatus.Completed;
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
        {
            chunk.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            chunk.Status = DownloadStatus.Failed;
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                throw new Exception($"下载失败: {ex.Message}", ex);
            }
        }
    }

    private async Task MergeChunks()
    {
        try
        {
            using var outputStream = new FileStream(_taskInfo.SavePath, FileMode.Create, FileAccess.Write);

            foreach (var chunk in _chunks.OrderBy(c => c.Index))
            {
                if (File.Exists(chunk.TempFilePath))
                {
                    using var inputStream = new FileStream(chunk.TempFilePath, FileMode.Open, FileAccess.Read);
                    await inputStream.CopyToAsync(outputStream);
                    inputStream.Close();

                    // 删除临时文件
                    try
                    {
                        File.Delete(chunk.TempFilePath);
                    }
                    catch
                    {
                        // 忽略删除失败
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"合并文件失败: {ex.Message}", ex);
        }
    }

    private async Task MergeSingleFile()
    {
        try
        {
            var tempFilePath = $"{_taskInfo.SavePath}.part0";
            if (File.Exists(tempFilePath))
            {
                File.Move(tempFilePath, _taskInfo.SavePath, true);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"重命名文件失败: {ex.Message}", ex);
        }
    }

    private async Task ReportProgressAsync()
    {
        _lastProgressTime = DateTime.Now;
        _lastProgressReport = 0;

        try
        {
            while (!_isDownloadCompleted && !_cancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(1000);

                if (_taskInfo?.Status != DownloadStatus.Downloading)
                    continue;

                lock (_progressLock)
                {
                    // 再次检查状态
                    if (_disposed || _taskInfo == null || _isDownloadCompleted)
                        break;

                    var currentTime = DateTime.Now;
                    var timeElapsed = (currentTime - _lastProgressTime).TotalSeconds;
                    var bytesDownloaded = _taskInfo.DownloadedSize - _lastProgressReport;

                    var speed = timeElapsed > 0 ? bytesDownloaded / timeElapsed : 0;
                    var progressPercentage = _taskInfo.TotalSize > 0 ?
                        (double)_taskInfo.DownloadedSize / _taskInfo.TotalSize * 100 : 0;

                    var remainingBytes = _taskInfo.TotalSize > 0 ?
                        _taskInfo.TotalSize - _taskInfo.DownloadedSize : bytesDownloaded;
                    var remainingTime = speed > 0 ? TimeSpan.FromSeconds(remainingBytes / speed) : TimeSpan.Zero;

                    try
                    {
                        OnProgressChanged(new DownloadProgressEventArgs
                        {
                            Url = _taskInfo.Url,
                            TotalSize = _taskInfo.TotalSize,
                            DownloadedSize = _taskInfo.DownloadedSize,
                            ProgressPercentage = progressPercentage,
                            Speed = speed,
                            RemainingTime = remainingTime
                        });
                    }
                    catch
                    {
                        // 忽略事件处理异常
                    }

                    _lastProgressTime = currentTime;
                    _lastProgressReport = _taskInfo.DownloadedSize;
                }
            }
        }
        catch (Exception)
        {
            // 忽略进度报告中的所有异常
        }
    }

    public void Pause()
    {
        _cancellationTokenSource.Cancel();
        if (_taskInfo != null)
        {
            _taskInfo.Status = DownloadStatus.Paused;
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
        if (_taskInfo != null)
        {
            _taskInfo.Status = DownloadStatus.Cancelled;
        }

        // 清理临时文件
        foreach (var chunk in _chunks)
        {
            if (File.Exists(chunk.TempFilePath))
            {
                try
                {
                    File.Delete(chunk.TempFilePath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
        }
    }

    protected virtual void OnProgressChanged(DownloadProgressEventArgs e)
    {
        try
        {
            ProgressChanged?.Invoke(this, e);
        }
        catch
        {
            // 忽略事件处理中的异常
        }
    }

    protected virtual void OnDownloadCompleted(DownloadCompletedEventArgs e)
    {
        try
        {
            DownloadCompleted?.Invoke(this, e);
        }
        catch
        {
            // 忽略事件处理中的异常
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 标记下载完成
                _isDownloadCompleted = true;

                // 取消所有操作
                try
                {
                    _cancellationTokenSource?.Cancel();
                }
                catch
                {
                    // 忽略取消异常
                }

                // 等待进度任务完成
                try
                {
                    if (_progressTask != null && !_progressTask.IsCompleted)
                    {
                        Task.WaitAny(_progressTask, Task.Delay(1000));
                    }
                }
                catch
                {
                    // 忽略等待异常
                }

                // 释放资源
                try
                {
                    _cancellationTokenSource?.Dispose();
                }
                catch
                {
                    // 忽略释放异常
                }

                try
                {
                    _httpClient?.Dispose();
                }
                catch
                {
                    // 忽略释放异常
                }
            }
            _disposed = true;
        }
    }
}
