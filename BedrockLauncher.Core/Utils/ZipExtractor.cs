using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace BedrockLauncher.Core.Utils
{
	public class ZipExtractor
	{
		

		public static async Task ExtractWithProgressAsync(
			string zipPath,
			string extractPath,
			IProgress<DecompressProgress>? progress = null,
			CancellationToken cancellationToken = default)
		{
			using var archive = ZipFile.OpenRead(zipPath);
			var entries = archive.Entries.ToArray();
			var totalFiles = entries.Length;

			var progressData = new DecompressProgress()
			{
				TotalCount = totalFiles,
				CurrentCount = 0,
				FileName = string.Empty
			};

			progress?.Report(progressData);

			var processedCount = 0;
			var progressLock = new object();

			var parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = Environment.ProcessorCount
			};

			await Task.Run(() =>
			{
				Parallel.ForEach(entries, parallelOptions, (entry, state) =>
				{
					cancellationToken.ThrowIfCancellationRequested();

					try
					{
						lock (progressLock)
						{
							progressData.FileName = entry.FullName;
							progress?.Report(progressData);
						}

						var destinationPath = Path.Combine(extractPath, entry.FullName);

						var directory = Path.GetDirectoryName(destinationPath);
						if (!string.IsNullOrEmpty(directory))
						{
							Directory.CreateDirectory(directory);
						}
						if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
							return;

						entry.ExtractToFile(destinationPath, overwrite: true);

						var newCount = Interlocked.Increment(ref processedCount);
						lock (progressLock)
						{
							progressData.CurrentCount = newCount;
							progress?.Report(progressData);
						}
					}
					catch 
					{
						throw new IOException($"Failed to Extract {progressData.FileName}");
					}
				});
			});

			progressData.TotalCount = totalFiles;
			progress?.Report(progressData);
		}
	}
}
