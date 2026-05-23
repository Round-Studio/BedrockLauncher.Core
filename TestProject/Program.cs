using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using System.Diagnostics;

namespace TestProject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var bedrockCore = new BedrockCore();
            bedrockCore.InitAsync().Wait();
            var launchOptions = new LaunchOptions()
            {
                GameFolder = Path.GetFullPath(@"E:\Bedrock\bedrock_versions\1.21.11401"),
                MinecraftBuildType = MinecraftBuildTypeVersion.UWP,
                GameType = MinecraftGameTypeVersion.Release,
                LaunchArgs = null
            };
            var process = bedrockCore.LaunchGameAsync(launchOptions).Result;
            Console.WriteLine(process.Id);
        }
    }
}
