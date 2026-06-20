using System;
using System.Collections.Generic;
using System.Text;
using Windows.Management.Deployment;
using BedrockLauncher.Core.BackGround;
using BedrockLauncher.Core.Utils;

namespace BedrockLauncher.Core.CoreOption
{
	/// <summary>
	/// Represents the options for installing a local game package
	/// </summary>
	public class LocalGamePackageOptions
	{
		/// <summary>
		/// Gets or sets the full file path of the game package to install
		/// </summary>
		public required string FileFullPath;

		/// <summary>
		/// Gets or sets the type of Minecraft build (GDK or UWP)
		/// </summary>
		public required MinecraftBuildTypeVersion Type;

		/// <summary>
		/// Gets or sets the destination folder where the package will be installed
		/// </summary>
		public required string InstallDstFolder;

		/// <summary>
		/// Gets or sets the progress reporter for extraction operations
		/// </summary>
		public Progress<DecompressProgress>? ExtractionProgress;

		/// <summary>
		/// Gets or sets the progress reporter for deployment operations
		/// </summary>
		public Progress<DeploymentProgress>? DeployProgress;

		/// <summary>
		/// Gets or sets the progress reporter for installation states
		/// </summary>
		public IProgress<InstallStates>? InstallStates;

		/// <summary>
		/// Gets or sets the cancellation token for the installation operation
		/// </summary>
		public CancellationToken? CancellationToken;

		/// <summary>
		/// Gets or sets the game type version (Release, Preview, or Beta)
		/// </summary>
		public required MinecraftGameTypeVersion GameTypeVersion;

		/// <summary>
		/// Gets or sets the background configuration for the installation
		/// </summary>
		public BackGroundConfig? BackGroundConfig;
		/// <summary>
		/// Title Name only being used When it is in Uwp mode
		/// </summary>
		public string? GameName;

		/// <summary>
		/// Gets or sets whether to use hardware-accelerated AES-NI/SSE2 decoding for GDK packages.
		/// Set to false to use software-based AES decryption (compatible with CPUs lacking AES-NI support).
		/// Default is true (hardware decode).
		/// </summary>
		public bool UseHardwareDecode { get; set; } = true;
	}

	/// <summary>
	/// Represents the different states during the game installation process
	/// </summary>
	public enum InstallStates
	{
		/// <summary>
		/// The package is being extracted
		/// </summary>
		Extracting,

		/// <summary>
		/// The package has been successfully extracted
		/// </summary>
		Extracted,

		/// <summary>
		/// The package is being registered with the system
		/// </summary>
		Registering,

		/// <summary>
		/// The package has been successfully registered
		/// </summary>
		Registered,

		/// <summary>
		/// The installation is being cleaned up
		/// </summary>
		Clearing,

		/// <summary>
		/// The installation has been successfully cleaned up
		/// </summary>
		Cleared
	}
}