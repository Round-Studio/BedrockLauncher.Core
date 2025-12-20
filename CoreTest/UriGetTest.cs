using System.Runtime.InteropServices;
using BedrockLauncher.Core;
using BedrockLauncher.Core.VersionJsons;

namespace CoreTest;

[TestClass]
public class UriGetTest
{
    [TestMethod]
    public void Test()
    {
	    var bedrockCore = new BedrockCore();
	    bedrockCore.InitAsync().Wait();
	    var	buildDatabaseAsync = VersionsHelper.GetBuildDatabaseAsync("https://data.mcappx.com/v2/bedrock.json").Result;
	    var result = bedrockCore.GetPackageUri(buildDatabaseAsync.Builds["1.21.114"],Architecture.X64).Result;
        Console.WriteLine(result);
    }
}
