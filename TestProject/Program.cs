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
                GameFolder = "D:\\BedrockBoot\\bedrock_versions\\1.21.11401",
                GameType = MinecraftGameTypeVersion.Release,
                MinecraftBuildType = MinecraftBuildTypeVersion.UWP,
                LaunchArgs = "minecraft://creator/?Editor=true"
            };
            var process = bedrockCore.LaunchGameAsync(launchOptions).Result;
            Console.WriteLine(process.Id);
        }
    }
}
