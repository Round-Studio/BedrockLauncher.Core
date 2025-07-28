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

namespace BedrockLauncherExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var bedrockCore = new BedrockCore();
                CoreOptions options = new CoreOptions();
                options.autoCompleteVC = true;
                options.autoOpenWindowsDevelopment = true;
                options.localDir = Path.Combine(Directory.GetCurrentDirectory(), "Versions");
                bedrockCore.options = options;
                var versionInformations = VersionHelper.GetVersions(bedrockCore.client, "https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/raw/main/data.json");
                int i = 0;
                versionInformations.ForEach((a) =>
                {
                    Console.WriteLine(a.ID + $"[{i}]" + a.Type);
                    i++;
                });

                var readLine = Console.ReadLine();
                var i1 = int.Parse(readLine);
                bedrockCore.Init();
                var b = bedrockCore.InstallVersion(versionInformations[i1], versionInformations[i1].ID, (new Progress<DownloadProgress>((
                        p =>
                        {
                            if (p.TotalBytes > 0)
                            {
                                Console.Write($"\r下载进度: {p.ProgressPercentage:F2}% ({p.DownloadedBytes / (1024.0 * 1024):F2} MB / {p.TotalBytes / (1024.0 * 1024):F2} MB)");
                            }
                            else
                            {
                                Console.Write($"\r已下载: {p.DownloadedBytes / (1024.0 * 1024):F2} MB (总大小未知)");
                            }
                        }))), ((s, u) =>
                    {
                        Console.WriteLine($"{s} -> {u}");
                    }),
                    ((status, exception) =>
                    {
                        Console.WriteLine(status);
                        Console.WriteLine(exception.Message);
                    }));
                Console.WriteLine(b);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
