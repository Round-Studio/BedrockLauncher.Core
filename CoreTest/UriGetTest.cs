using System.Runtime.InteropServices;
using BedrockLauncher.Core;
using BedrockLauncher.Core.VersionJsons;

namespace CoreTest;

[TestClass]
public class UriGetTest
{
    [TestMethod]
    public async Task TestAsync()
    {
	    var bedrockCore = new BedrockCore();
	    bedrockCore.InitAsync().Wait();
	    var	buildDatabaseAsync = VersionsHelper.GetBuildDatabaseAsync("https://data.mcappx.com/v2/bedrock.json").Result;
	    BuildInfo build = null;
		await foreach (var kvp in buildDatabaseAsync.Builds)
		{
			if (kvp.Key == "1.21.131")
				build = kvp.Value;
		}
		var result = bedrockCore.GetPackageUri(build,Architecture.X64).Result;
        Console.WriteLine(result);
    }
}
