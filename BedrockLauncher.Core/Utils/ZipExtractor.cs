using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.Utils
{

	public static class ZipExtractor
	{
		/// <summary>
		/// Extracts a ZIP archive to the specified directory with progress reporting
		/// </summary>
		/// <param name="zipPath">Path to the ZIP archive</param>
		/// <param name="extractPath">Directory where files will be extracted</param>
		/// <param name="progress">Progress reporter for extraction status</param>
		/// <exception cref="FileNotFoundException">Thrown when ZIP file does not exist</exception>
		/// <exception cref="InvalidOperationException">Thrown when ZIP contains potential path traversal attacks</exception>
		public static async Task ExtractWithProgressAsync(string zipPath, string extractPath, IProgress<DecompressProgress>? progress, CancellationToken cancellationToken = default)
		{
			// Validate input file exists
			if (!File.Exists(zipPath))
				throw new FileNotFoundException("ZIP file not found", zipPath);

			// Ensure target directory exists
			Directory.CreateDirectory(extractPath);

			// Count total files in the archive
			int totalFiles;
			using (var archive = ZipFile.OpenRead(zipPath))
			{
				totalFiles = archive.Entries.Count;
			}

			var decompressProgress = new DecompressProgress
			{
				TotalCount = totalFiles,
				CurrentCount = 0,
				FileName = string.Empty
			};

			// Re-open archive for extraction
			using (var archive = ZipFile.OpenRead(zipPath))
			{
				foreach (var entry in archive.Entries)
				{
					// Check for cancellation
					cancellationToken.ThrowIfCancellationRequested();

					// Skip directory entries (entries without a name)
					if (!string.IsNullOrEmpty(entry.Name))
					{
						// Get full destination path
						var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

						// Prevent path traversal attacks
						if (!destinationPath.StartsWith(extractPath, StringComparison.OrdinalIgnoreCase))
							throw new InvalidOperationException("ZIP file contains potential path traversal attacks.");

						// Ensure destination directory exists
						var destinationDir = Path.GetDirectoryName(destinationPath);
						if (!string.IsNullOrEmpty(destinationDir))
							Directory.CreateDirectory(destinationDir);

						// Extract file using streams
						using (var sourceStream = entry.Open())
						using (var targetStream = File.Create(destinationPath))
						{
							// Use async copy method
							await sourceStream.CopyToAsync(targetStream, cancellationToken);
						}
					}

					// Update progress information
					decompressProgress.CurrentCount++;
					decompressProgress.FileName = entry.FullName;

					// Report progress
					progress?.Report(decompressProgress);
				}
			}
		}
	}
}