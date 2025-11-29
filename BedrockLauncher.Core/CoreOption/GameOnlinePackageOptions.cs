using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockLauncher.Core.CoreOption
{
	/// <summary>
	/// Represents the set of options used to configure an online game package operation, including file paths, build
	/// information, architecture, download settings, and cancellation support.
	/// </summary>
	/// <remarks>Use this class to specify parameters when initiating an online game package process, such as where to
	/// save files, which architecture to target, and how to handle download progress and cancellation. All required
	/// properties must be set before use.</remarks>
	public class GameOnlinePackageOptions
	{
		/// <summary>
		/// Gets or sets the file path where data is saved.
		/// </summary>
		public required string SaveFilePath;
		/// <summary>
		/// Specifies the processor architecture, or null if the architecture is unspecified.
		/// </summary>
		public Architecture? Architecture;
		/// <summary>
		/// Gets or sets the build information for the application.
		/// </summary>
		public required BuildInfo BuildInfo;
		/// <summary>
		/// Gets or sets the identifier of the download thread, if available.
		/// </summary>
		public int? DownloadThread;
		/// <summary>
		/// Gets or sets the progress reporter for download operations.
		/// </summary>
		/// <remarks>Use this property to receive progress updates during a download. The progress reporter receives
		/// updates as <see cref="DownloadProgress"/> values, which typically indicate the number of bytes downloaded and the
		/// total size, if known. If this property is <see langword="null"/>, no progress updates are reported.</remarks>
		public Progress<DownloadProgress>? DownloadProgress;
		/// <summary>
		/// Gets or sets the optional cancellation token that can be used to cancel the associated operation.
		/// </summary>
		/// <remarks>If a value is provided, the operation will observe cancellation requests signaled through this
		/// token. If null, the operation cannot be cancelled via a token.</remarks>
		public CancellationToken? CancellationToken;
		/// <summary>
		/// Max RetryTimes
		/// </summary>
		public int? MaxRetryTimes;
	}
}
