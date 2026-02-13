using System.Diagnostics;
using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.DependsComplete;
using BedrockLauncher.Core.Utils;

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
            GameFolder = Path.GetFullPath(@"E:\test2\plugins\Glacie\bedrock_versions\1.26.2"),
            MinecraftBuildType = MinecraftBuildTypeVersion.GDK,
            GameType = MinecraftGameTypeVersion.Release,
            LaunchArgs = null
        };
        var process = bedrockCore.LaunchGameAsync(launchOptions).Result;
        Debug.WriteLine(process.Id);
      //   ExeLauncher.LaunchWithLowPrivilege(@"E:\GlacieCrack\plugins\Glacie\bedrock_versions\1234\Minecraft.Windows.exe", "");
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
