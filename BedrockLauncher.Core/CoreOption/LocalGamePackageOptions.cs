using System;
using System.Collections.Generic;
using System.Text;
using Windows.Management.Deployment;
using BedrockLauncher.Core.BackGround;
using BedrockLauncher.Core.Utils;

namespace BedrockLauncher.Core.CoreOption
{
	public class LocalGamePackageOptions
	{
		public required string FileFullPath;
		public required MinecraftBuildTypeVersion Type;
		public required string InstallDstFolder;
		public Progress<DecompressProgress>? ExtractionProgress;
		public Progress<DeploymentProgress>? DeployProgress;
	    public IProgress<InstallStates>? InstallStates;
		public CancellationToken? CancellationToken;
		public required MinecraftGameTypeVersion GameTypeVersion;
		public BackGroundConfig? BackGroundConfig;
		public DeploymentResult? DeploymentResult;
	}
	public enum InstallStates
	{
		Extracting,
		Extracted,
		Registering,
		Registered,
		Clearing,
		Cleared
	}
}
