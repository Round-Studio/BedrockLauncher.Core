using BedrockLauncher.Core;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.FrameworkComplete;
using BedrockLauncher.Core.Native;
using BedrockLauncher.Core.Network;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Windows.Foundation;
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
                var versionInformations = VersionHelper.GetVersions(bedrockCore.client, "https://data.mcappx.com/v1/bedrock.json");

                int i = 0;
                versionInformations.ForEach((a) =>
                {
                    Console.WriteLine(a.ID + $"[{i}]" + a.Type);
                    i++;
                });

                var readLine = Console.ReadLine();
                var i1 = int.Parse(readLine);
                bedrockCore.downloader.ProgressChanged += Downloader_ProgressChanged;
                bedrockCore.downloader.DownloadCompleted += Downloader_DownloadCompleted;
                bedrockCore.Init();
                var b = bedrockCore.InstallVersion(versionInformations[i1], versionInformations[i1].ID, ((s, u) =>
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
           
            //Wait for complete
        }

        private static void Downloader_DownloadCompleted(object? sender, DownloadCompletedEventArgs e)
        {
           Console.WriteLine("下载完成开始解压");
        }

        private static void Downloader_ProgressChanged(object? sender, DownloadProgressEventArgs e)
        {
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 3);
            defaultInterpolatedStringHandler.AppendLiteral("进度: ");
            defaultInterpolatedStringHandler.AppendFormatted<double>(e.ProgressPercentage, "F2");
            defaultInterpolatedStringHandler.AppendLiteral("% ");
            defaultInterpolatedStringHandler.AppendLiteral("速度: ");
            defaultInterpolatedStringHandler.AppendFormatted<double>(e.Speed / 1024.0 / 1024.0, "F2");
            defaultInterpolatedStringHandler.AppendLiteral(" MB/s ");
            defaultInterpolatedStringHandler.AppendLiteral("剩余时间: ");
            defaultInterpolatedStringHandler.AppendFormatted<TimeSpan>(e.RemainingTime, "mm\\:ss");
            Console.Write(defaultInterpolatedStringHandler.ToStringAndClear());
        }
    }
}