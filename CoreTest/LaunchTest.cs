using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.DependsComplete;

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
            GameFolder = Path.GetFullPath("D:\\Windows11\\newdesk\\Code\\bedrock_versions\\1.21.13101"),
            MinecraftBuildType = MinecraftBuildTypeVersion.GDK,
            GameType = MinecraftGameTypeVersion.Release,
             LaunchArgs        = null
		};
        bedrockCore.LaunchGameAsync(launchOptions).Wait();
    }
    [TestMethod]
    public void MsiTest()
    {
        var bedrockCore = new BedrockCore();
        var installGameInput = VCRuntimeHelper.InstallGameInput();
        installGameInput.Wait();
    }
    [TestMethod]
    public void GameInputInstallTest()
    {
        var bedrockCore = new BedrockCore();
         bedrockCore.AutoCompleteGameInput().Wait();
    }
}
