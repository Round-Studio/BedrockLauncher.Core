using System;
using System.Collections.Generic;
using System.Text;
using Windows.Management.Deployment;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.VersionJsons;

namespace CoreTest
{
	[TestClass]
	public sealed class InstallTest
	{
		[TestMethod]
		public void Test()
		{
			var bedrockCore = new BedrockCore();
			bedrockCore.InitAsync().Wait();
			var localGamePackageOptions = new LocalGamePackageOptions()
			{
				DeployProgress = (new Progress<DeploymentProgress>((progress =>
				{
					Console.WriteLine(progress.percentage);
				}))),
				Type = MinecraftBuildTypeVersion.UWP,
				GameTypeVersion = MinecraftGameTypeVersion.Release,
				InstallDstFolder = Path.GetFullPath("./Test7829"),
				GameName = "88991",
				FileFullPath = @"D:\Windows11\Download\Microsoft.MinecraftUWP_1.7.100.0_x64__8wekyb3d8bbwe.Appx"
			};
			bedrockCore.InstallPackageAsync(localGamePackageOptions).Wait();
			//var launchOptions = new LaunchOptions()
			//{
			//	GameFolder = Path.GetFullPath("./Test78"),
			//	GameType = MinecraftGameTypeVersion.Release,
			//	MinecraftBuildType = MinecraftBuildTypeVersion.UWP,
			//};
			//var process = bedrockCore.LaunchGameAsync(launchOptions).Result;
			//Assert.IsNotNull(process);
		}
	}
}
