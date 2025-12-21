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
            GameFolder = Path.GetFullPath("D:\\Windows11\\newdesk\\Code\\bedrock_versions\\1.21.11401"),
            MinecraftBuildType = MinecraftBuildTypeVersion.UWP,
            GameType = MinecraftGameTypeVersion.Release,
             LaunchArgs        = null
		};
        bedrockCore.StartGameAsync(launchOptions).Wait();
    }
}
