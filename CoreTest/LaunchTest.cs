using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;

namespace CoreTest;

[TestClass]
public class LaunchTest
{
    [TestMethod]
    public void Test()
    {
        var bedrockCore = new BedrockCore();
        bedrockCore.InitAsync().Wait();
        var launchOptions = new LaunchOptions()
        {
            GameFolder = Path.GetFullPath("C:\\Users\\Administrator\\AppData\\Roaming\\RoundStudio\\BedrockBoot\\Bedrock_Data\\bedrock_versions\\1.21.120202"),
            MinecraftBuildType = MinecraftBuildTypeVersion.UWP,
            GameType = MinecraftGameTypeVersion.Preview,
            LaunchArgs = "minecraft://creator/?Editor=true"
		};
        bedrockCore.StartGameAsync(launchOptions).Wait();
    }
}
