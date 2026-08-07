using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using Windows.Management.Deployment;
using BedrockLauncher.Core.Utils;

namespace TestProject
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var bedrockCore = new BedrockCore();
            bedrockCore.InitAsync().Wait();

            /*bedrockCore.InstallPackageAsync(new LocalGamePackageOptions()
            {
                FileFullPath = @"D:\BedrockBoot\version_save\1.26.2.insPack",
                Type = MinecraftBuildTypeVersion.GDK,
                GameTypeVersion = MinecraftGameTypeVersion.Release,
                InstallDstFolder = Path.GetFullPath(@"E:\GDK_TestInstall\1.26_2"),
                UseHardwareDecode = false,
                DeployProgress = new Progress<DeploymentProgress>(p =>
                    Console.WriteLine($"Deploy: {p.percentage}%")),
                ExtractionProgress = new Progress<DecompressProgress>(p =>
                    Console.WriteLine($"Extract: [{p.CurrentCount}/{p.TotalCount}] {p.FileName}")),
                InstallStates = new Progress<InstallStates>(s =>
                    Console.WriteLine($"State: {s}")),
            }).Wait();*/

            var proc = await bedrockCore.LaunchGameAsync(new()
            {
                MinecraftBuildType = MinecraftBuildTypeVersion.GDK,
                GameFolder = "D:\\BedrockBoot\\bedrock_versions\\1.26.2",
                GameType = MinecraftGameTypeVersion.Release,
                LaunchArgs = "minecraft://creator/?Editor=true",
                RunAsAdministrator = true
            });
            Console.WriteLine("Install done.");
        }
    }
}
