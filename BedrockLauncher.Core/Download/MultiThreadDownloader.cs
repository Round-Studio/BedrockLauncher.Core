﻿using System.Net.Http.Headers;

public class ImprovedFlexibleMultiThreadDownloader : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly int _maxConcurrency;
    private readonly int _bufferSize;
    private readonly TimeSpan _defaultTimeout; // 用于存储默认超时时间
    private const long ProgressReportThresholdBytes = 100 * 1024;
    private static readonly TimeSpan ProgressReportInterval = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="maxConcurrency">最大并发下载线程数</param>
    /// <param name="bufferSize">每次读写操作的缓冲区大小</param>
    /// <param name="defaultTimeoutSeconds">默认的单个 HTTP 请求超时时间（秒）默认为 100 秒</param>
    public ImprovedFlexibleMultiThreadDownloader(int maxConcurrency = 4, int bufferSize = 81920, int defaultTimeoutSeconds = 100)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        _httpClient = new HttpClient(handler);
        // 设置 HttpClient 的默认超时时间
        _defaultTimeout = TimeSpan.FromSeconds(defaultTimeoutSeconds);
        _httpClient.Timeout = _defaultTimeout;

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ImprovedFlexibleDownloader/1.0)");
        _maxConcurrency = maxConcurrency;
        _bufferSize = bufferSize;
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="url">要下载的文件的 URL</param>
    /// <param name="filePath">保存文件的本地路径</param>
    /// <param name="progress">用于报告下载进度的回调</param>
    /// <param name="cancellationToken">用于取消操作的令牌</param>
    /// <returns>如果下载成功则返回 true，否则返回 false</returns>
    public async Task<bool> DownloadAsync(string url, string filePath, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("错误: URL 不能为空或空白");
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("错误: 文件路径不能为空或空白");
            return false;
        }

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException ex)
        {
            Console.WriteLine($"错误: URL 格式无效{ex.Message}");
            return false; 
        }

        try
        {
            var (fileSize, supportsRange) = await GetFileInfoAsync(uri, cancellationToken);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (fileSize > 0 && supportsRange)
            {
                Console.WriteLine($"服务器支持断点续传且文件大小已知 ({fileSize} bytes)，使用多线程下载 (线程数: {_maxConcurrency})...");
                await DownloadMultiPartAsync(uri, filePath, fileSize, progress, cancellationToken);
            }
            else if (fileSize > 0 && !supportsRange)
            {
                Console.WriteLine($"文件大小已知 ({fileSize} bytes) 但服务器不支持断点续传，使用单线程下载...");
                await DownloadSinglePartAsync(uri, filePath, fileSize, progress, cancellationToken);
            }
            else
            {
                Console.WriteLine("无法获取文件大小或服务器不支持必要的功能，使用流式单线程下载 (无法显示进度百分比)...");
                await DownloadAsStreamAsync(uri, filePath, progress, cancellationToken);
            }

            // 如果执行到这里没有抛出异常，说明下载成功
            Console.WriteLine($"文件已成功下载并保存到: {filePath}");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("下载被用户取消");
            return false; // 取消操作视为失败
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || (ex.InnerException == null && ex.CancellationToken == default))
        {
            // 注意：HttpClient.Timeout 导致的超时通常抛出 TaskCanceledException
            // 如果是默认 CancellationToken 且被取消，很可能是超时
            Console.WriteLine($"下载失败: 请求超时 (超过 {_defaultTimeout.TotalSeconds} 秒)");
            return false;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"下载失败: 网络请求错误{ex.Message}");
            return false;
        }
        catch (Exception ex) // 捕获其他所有未预见的异常
        {
            Console.WriteLine($"下载过程中发生未预期的错误: {ex}");
            // 可以选择重新抛出未预见的异常，或者像下面这样记录并返回 false
            // throw; // 如果希望调用者处理未预见异常，可以取消注释
            return false; // 将未预见异常也视为失败
        }
    }

    private async Task<(long fileSize, bool supportsRange)> GetFileInfoAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (headResponse.IsSuccessStatusCode)
            {
                var contentLength = headResponse.Content.Headers.ContentLength ?? -1;
                var supportsRange = headResponse.Headers.AcceptRanges?.ToString().Equals("bytes", StringComparison.OrdinalIgnoreCase) == true;
                return (contentLength, supportsRange);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HEAD 请求失败: {ex.Message}");
        }
        try
        {
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            getRequest.Headers.Range = new RangeHeaderValue(0, 1);
            using var getResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (getResponse.IsSuccessStatusCode)
            {
                var contentRange = getResponse.Content.Headers.ContentRange;
                var supportsRange = contentRange != null && contentRange.HasLength;
                var contentLength = contentRange?.Length ?? getResponse.Content.Headers.ContentLength ?? -1;
                return (contentLength, supportsRange);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"带 Range 的 GET 请求失败: {ex.Message}");
        }
        try
        {
            using var fullRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            using var fullResponse = await _httpClient.SendAsync(fullRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (fullResponse.IsSuccessStatusCode)
            {
                var contentLength = fullResponse.Content.Headers.ContentLength ?? -1;
                return (contentLength, false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"完整 GET 请求失败: {ex.Message}");
        }
        return (-1, false);
    }

    private async Task DownloadMultiPartAsync(Uri uri, string filePath, long fileSize, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        var tempDir = Path.GetTempPath();
        var guid = Guid.NewGuid().ToString("N");
        var tempFilePrefix = Path.Combine(tempDir, $"dl_{guid}_part");
        var partSize = fileSize / _maxConcurrency;
        var tasks = new List<Task>();
        var tempFiles = new string[_maxConcurrency];
        var totalDownloadedBytes = 0L;
        var lastReportedBytes = 0L;
        var lastReportTime = DateTimeOffset.UtcNow;
        var locker = new object();

        void ReportProgressIfNeeded(bool force = false)
        {
            var currentTotal = Interlocked.Read(ref totalDownloadedBytes);
            var now = DateTimeOffset.UtcNow;
            bool shouldReport = force ||
                                (currentTotal - lastReportedBytes) >= ProgressReportThresholdBytes ||
                                (now - lastReportTime) >= ProgressReportInterval;
            if (shouldReport)
            {
                lock (locker)
                {
                    var currentTotalCheck = Interlocked.Read(ref totalDownloadedBytes);
                    var nowCheck = DateTimeOffset.UtcNow;
                    if (force || (currentTotalCheck - lastReportedBytes) >= ProgressReportThresholdBytes || (nowCheck - lastReportTime) >= ProgressReportInterval)
                    {
                        lastReportedBytes = currentTotalCheck;
                        lastReportTime = nowCheck;
                        progress?.Report(new DownloadProgress
                        {
                            TotalBytes = fileSize,
                            DownloadedBytes = currentTotalCheck
                        });
                    }
                }
            }
        }

        using var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        for (int i = 0; i < _maxConcurrency; i++)
        {
            var start = i * partSize;
            var end = (i == _maxConcurrency - 1) ? fileSize - 1 : start + partSize - 1;
            var tempFilePath = $"{tempFilePrefix}{i}.tmp";
            tempFiles[i] = tempFilePath;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Range = new RangeHeaderValue(start, end);
                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true);
                    var buffer = new byte[_bufferSize];
                    int bytesRead;
                    long localDownloaded = 0;
                    while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                        var newTotal = Interlocked.Add(ref totalDownloadedBytes, bytesRead);
                        localDownloaded += bytesRead;
                        ReportProgressIfNeeded(); // 节流报告
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
            Console.WriteLine("所有分块下载完成，正在合并文件...");
            await MergeTempFilesAsync(tempFiles, filePath, cancellationToken);
            // 强制报告最终 100% 进度
            progress?.Report(new DownloadProgress
            {
                TotalBytes = fileSize,
                DownloadedBytes = fileSize
            });
            Console.WriteLine($"文件合并完成: {filePath}");
        }
        catch (Exception)
        {
            CleanupTempFiles(tempFiles);
            throw; // 重新抛出异常，让 DownloadAsync 捕获
        }
        finally
        {
            CleanupTempFiles(tempFiles);
        }
    }

    private async Task DownloadSinglePartAsync(Uri uri, string filePath, long fileSize, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        // HttpClient 已经设置了 Timeout
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode(); // 这会抛出 HttpRequestException 如果状态码不是成功码

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true);

        var buffer = new byte[_bufferSize];
        int bytesRead;
        long totalBytesRead = 0;
        long lastReportedBytes = 0;
        var lastReportTime = DateTimeOffset.UtcNow;

        void ReportProgressIfNeeded()
        {
            bool shouldReport =
                (totalBytesRead - lastReportedBytes) >= ProgressReportThresholdBytes ||
                (DateTimeOffset.UtcNow - lastReportTime) >= ProgressReportInterval;
            if (shouldReport)
            {
                lastReportedBytes = totalBytesRead;
                lastReportTime = DateTimeOffset.UtcNow;
                progress?.Report(new DownloadProgress
                {
                    TotalBytes = fileSize,
                    DownloadedBytes = totalBytesRead
                });
            }
        }

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            ReportProgressIfNeeded();
        }

        // 报告最终进度
        progress?.Report(new DownloadProgress
        {
            TotalBytes = fileSize,
            DownloadedBytes = totalBytesRead
        });
    }

    private async Task DownloadAsStreamAsync(Uri uri, string filePath, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        // HttpClient 已经设置了 Timeout
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true);

        var buffer = new byte[_bufferSize];
        int bytesRead;
        long totalBytesRead = 0;
        long lastReportedBytes = 0;
        var lastReportTime = DateTimeOffset.UtcNow;

        void ReportProgressIfNeeded()
        {
            bool shouldReport =
               (totalBytesRead - lastReportedBytes) >= ProgressReportThresholdBytes ||
               (DateTimeOffset.UtcNow - lastReportTime) >= ProgressReportInterval;
            if (shouldReport)
            {
                lastReportedBytes = totalBytesRead;
                lastReportTime = DateTimeOffset.UtcNow;
                progress?.Report(new DownloadProgress
                {
                    TotalBytes = -1,
                    DownloadedBytes = totalBytesRead
                });
            }
        }

        while ((bytesRead = await responseStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            ReportProgressIfNeeded();
        }

        // 报告最终进度
        progress?.Report(new DownloadProgress
        {
            TotalBytes = -1,
            DownloadedBytes = totalBytesRead
        });
        Console.WriteLine($"文件已成功下载并保存到: {filePath} (大小: {totalBytesRead} bytes)");
    }

    private async Task MergeTempFilesAsync(string[] tempFiles, string outputPath, CancellationToken cancellationToken)
    {
        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true);
        foreach (var tempFile in tempFiles)
        {
            cancellationToken.ThrowIfCancellationRequested(); // 检查取消
            if (File.Exists(tempFile))
            {
                using var inputStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, useAsync: true);
                await inputStream.CopyToAsync(outputStream, _bufferSize, cancellationToken);
            }
        }
    }

    private void CleanupTempFiles(string[] tempFiles)
    {
        foreach (var tempFile in tempFiles)
        {
            if (File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"清理临时文件失败 '{tempFile}': {ex.Message}");
                    // 不抛出异常，继续清理其他文件
                }
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}