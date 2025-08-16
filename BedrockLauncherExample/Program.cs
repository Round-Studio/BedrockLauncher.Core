using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.Native;
using BedrockLauncher.Core.Network;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using BedrockLauncher.Core.FrameworkComplete;
using BedrockLauncher.Core.JsonHandle;

namespace BedrockLauncherExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ManifestEditor.EditManifest("E:\\BedrockLauncher\\imported_versions\\Microsoft.MinecraftWindowsBeta_1.21.10024.0_x64__8wekyb3d8bbwe.Appx",null);
                // List<VersionInformation> versionInformations = VersionHelper.GetVersions("https://data.mcappx.com/v1/bedrock.json");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
