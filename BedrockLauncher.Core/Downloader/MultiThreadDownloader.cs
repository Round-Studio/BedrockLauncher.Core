using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public enum DownloadStage
{
	Downloading,
	Merging,
	Merged
}

public class DownloadProgress
{
	public double Progress { get; set; }
	public long FinishedBytes { get; set; }
	public long TotalBytes { get; set; }
	public double Speed { get; set; }
	public DownloadStage Phase { get; set; }
}

public class MultiThreadDownloader
{
	private readonly HttpClient _client = new HttpClient();

	/// <summary>
	/// Multithreaded downloader with progress, speed, retry and cleanup temp files.
	/// </summary>
	public async Task DownloadFileAsync(
		string url,
		string outputPath,
		int threadCount = 4,
		IProgress<DownloadProgress> progress = null,
		CancellationToken cancellationToken = default,
		int maxRetry = 3,
		int retryDelayMs = 2000)
	{
		var tempFiles = new List<string>();
		try
		{
			var headResponse = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), cancellationToken);
			headResponse.EnsureSuccessStatusCode();
			long totalBytes = headResponse.Content.Headers.ContentLength ?? throw new Exception("Content-Length header not found");
			if (headResponse.Headers.AcceptRanges == null || !headResponse.Headers.AcceptRanges.Contains("bytes"))
				throw new NotSupportedException("Server does not support range downloads.");

			long chunkSize = totalBytes / threadCount;
			long totalRead = 0;
			long mergedRead = 0;
			object progressLock = new object();

			DownloadStage phase = DownloadStage.Downloading;
			long lastTotal = 0;
			DateTime lastTick = DateTime.UtcNow;
			double lastSpeed = 0;

			var downloadTasks = new List<Task>();
			var sliceTotals = new long[threadCount];

			var reportTask = Task.Run(async () =>
			{
				while (true)
				{
					await Task.Delay(500, cancellationToken);

					long currentFinished, currentTotal;
					DownloadStage currPhase;
					lock (progressLock)
					{
						if (phase == DownloadStage.Downloading)
						{
							currentFinished = 0;
							for (int j = 0; j < threadCount; j++) currentFinished += sliceTotals[j];
						}
						else
						{
							currentFinished = Math.Min(totalRead + mergedRead, totalBytes);
						}
						currentTotal = totalBytes;
						currPhase = phase;
					}
					double speed = 0;
					var now = DateTime.UtcNow;
					if ((now - lastTick).TotalSeconds > 0.1)
					{
						speed = (currentFinished - lastTotal) / (now - lastTick).TotalSeconds;
						lastTick = now;
						lastTotal = currentFinished;
						lastSpeed = speed;
					}
					else
					{
						speed = lastSpeed;
					}
					double prog = Math.Min((double)currentFinished / Math.Max(1, currentTotal), 1.0);

					progress?.Report(new DownloadProgress
					{
						Progress = prog,
						FinishedBytes = currentFinished,
						TotalBytes = currentTotal,
						Speed = speed,
						Phase = currPhase
					});

					if (currPhase == DownloadStage.Merged)
						break;
				}
			}, cancellationToken);

			for (int i = 0; i < threadCount; i++)
			{
				int partIdx = i;
				long from = chunkSize * partIdx;
				long to = (partIdx == threadCount - 1) ? totalBytes - 1 : (chunkSize * (partIdx + 1)) - 1;
				string tempFile = Path.GetTempFileName();
				tempFiles.Add(tempFile);

				downloadTasks.Add(Task.Run(async () =>
				{
					await DownloadRangeToFileWithRetryAsync(
						url, from, to, tempFile, cancellationToken,
						onRead: bytesRead =>
						{
							lock (progressLock)
							{
								sliceTotals[partIdx] += bytesRead;
							}
						},
						maxRetry: maxRetry,
						retryDelayMs: retryDelayMs
					);
				}, cancellationToken));
			}

			await Task.WhenAll(downloadTasks);

			lock (progressLock)
			{
				totalRead = 0;
				for (int i = 0; i < threadCount; i++) totalRead += sliceTotals[i];
				phase = DownloadStage.Merging;
				lastTick = DateTime.UtcNow;
				lastTotal = 0;
				mergedRead = 0;
				lastSpeed = 0;
			}

			using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				foreach (var tempFile in tempFiles)
				{
					using (var input = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
					{
						byte[] buffer = new byte[81920];
						int read;
						while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
						{
							await output.WriteAsync(buffer, 0, read, cancellationToken);
							lock (progressLock) { mergedRead += read; }
						}
					}
					File.Delete(tempFile);
				}
			}

			lock (progressLock)
			{
				phase = DownloadStage.Merged;
			}
			progress?.Report(new DownloadProgress
			{
				Progress = 1.0,
				FinishedBytes = totalBytes,
				TotalBytes = totalBytes,
				Speed = 0,
				Phase = phase
			});

			await reportTask;
		}
		catch (OperationCanceledException)
		{
			foreach (var file in tempFiles)
			{
				try { if (File.Exists(file)) File.Delete(file); } catch { }
			}
			throw;
		}
		catch
		{
			foreach (var file in tempFiles)
			{
				try { if (File.Exists(file)) File.Delete(file); } catch { }
			}
			throw;
		}
	}

	private async Task DownloadRangeToFileWithRetryAsync(
		string url, long from, long to, string tempFile, CancellationToken cancellationToken,
		Action<long> onRead = null, int maxRetry = 3, int retryDelayMs = 2000)
	{
		int attempt = 0;
		Exception lastException = null;

		while (attempt < maxRetry)
		{
			try
			{
				if (attempt > 0 && File.Exists(tempFile))
					File.Delete(tempFile);

				await DownloadRangeToFileAsync(url, from, to, tempFile, cancellationToken, onRead);
				return;
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception ex) when (ex is IOException || ex is HttpRequestException)
			{
				lastException = ex;
				attempt++;
				if (attempt < maxRetry)
					await Task.Delay(retryDelayMs, cancellationToken);
			}
		}
		throw new Exception($"Part download failed (from:{from} to:{to}), retried {maxRetry} times.", lastException);
	}

	private async Task DownloadRangeToFileAsync(
		string url, long from, long to, string tempFile, CancellationToken cancellationToken, Action<long> onRead = null)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, url);
		request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, to);
		var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		response.EnsureSuccessStatusCode();

		using var src = await response.Content.ReadAsStreamAsync(cancellationToken);
		using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);

		byte[] buffer = new byte[81920];
		int read;
		while ((read = await src.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
		{
			await fs.WriteAsync(buffer, 0, read, cancellationToken);
			onRead?.Invoke(read);
		}
	}
}