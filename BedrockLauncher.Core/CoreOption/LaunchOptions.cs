using System;
using System.Collections.Generic;
using System.Text;
using Windows.Management.Deployment;

namespace BedrockLauncher.Core.CoreOption
{
	/// <summary>
	/// Represents the options for launching a Minecraft game instance
	/// </summary>
	public class LaunchOptions
	{
		/// <summary>
		/// Gets or sets the build type version of the Minecraft game (GDK or UWP)
		/// </summary>
		public required MinecraftBuildTypeVersion MinecraftBuildType;

		/// <summary>
		/// Gets or sets the game type version (Release, Preview, or Beta)
		/// </summary>
		public required MinecraftGameTypeVersion GameType;

		/// <summary>
		/// Gets or sets the folder path where the game is installed
		/// </summary>
		public required string GameFolder;

		/// <summary>
		/// Gets or sets the progress reporter for launch operations
		/// </summary>
		public IProgress<LaunchState>? Progress;

		/// <summary>
		/// Gets or sets the command line arguments to pass to the game executable
		/// </summary>
		public string? LaunchArgs;

		/// <summary>
		/// Gets or sets the cancellation token for the launch operation
		/// </summary>
		public CancellationToken? CancellationToken;

		/// <summary>
		/// Gets or sets the progress reporter for package registration operations
		/// </summary>
		public IProgress<DeploymentProgress>? RegisterProgress;
		/// <summary>
		/// Launching for old version
		/// </summary>
		public bool Old_VersionLaunching = false;
	}

	/// <summary>
	/// Represents the different states during the game launch process
	/// </summary>
	public enum LaunchState
	{
		/// <summary>
		/// The package is being registered with the system
		/// </summary>
		Registering,

		/// <summary>
		/// The package has been successfully registered
		/// </summary>
		Registered,

		/// <summary>
		/// The game is being launched
		/// </summary>
		Launching,

		/// <summary>
		/// The game has been successfully launched
		/// </summary>
		Launched
	}
}