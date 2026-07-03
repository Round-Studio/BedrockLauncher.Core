using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using Windows.Management.Deployment;
using BedrockLauncher.Core.Utils;

namespace TestProject
{
    internal class Program
    {
        static void Main(string[] args)
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

            bedrockCore.LaunchGameAsync(new()
            {
                MinecraftBuildType = MinecraftBuildTypeVersion.GDK,
                GameFolder = "E:\\Bedrock Test\\bedrock_versions\\1.26.3202",
                GameType = MinecraftGameTypeVersion.Release
            });
            Console.WriteLine("Install done.");
        }
    }
}
