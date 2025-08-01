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
                var coreOptions = new CoreOptions()
                {
                    localDir = Directory.GetCurrentDirectory()
                };
                bedrockCore.Options = coreOptions;
                bedrockCore.Init();
                //var versionInformations = VersionHelper.GetVersions("https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/raw/main/data.json");
                //int i = 0;
                //versionInformations.ForEach((a) =>
                //{
                //    Console.WriteLine(a.ID + $"[{i}]" + a.Type);
                //    i++;
                //});

                //var readLine = Console.ReadLine();
                //var i1 = int.Parse(readLine);
                //var cts = new CancellationTokenSource();
                //var keyPressTask = Task.Run(() =>
                //{
                //    ConsoleKeyInfo key;
                //    do
                //    {
                //        key = Console.ReadKey(true); // true 表示不回显按下的键
                //    } while (key.Key != ConsoleKey.C);

                //    Console.WriteLine("\n检测到 'C' 键，正在取消下载...");
                //    // 4. 触发取消
                //    cts.Cancel();
                //});
                //InstallCallback callback = new InstallCallback()
                //{
                //    zipProgress = new Progress<ZipProgress>((progress =>
                //    {
                //        Console.WriteLine(progress.ToString());
                //    })),
                //    CancellationToken = cts.Token,
                //    downloadProgress = (new Progress<DownloadProgress>((p =>
                //    {
                //        if (p.TotalBytes > 0)
                //        {
                //            Console.Write($"\r下载进度: {p.ProgressPercentage:F2}% ({p.DownloadedBytes / (1024.0 * 1024):F2} MB / {p.TotalBytes / (1024.0 * 1024):F2} MB)");
                //        }
                //        else
                //        {
                //            Console.Write($"\r已下载: {p.DownloadedBytes / (1024.0 * 1024):F2} MB (总大小未知)");
                //        }
                //    }))),
                //    registerProcess_percent = ((s, u) =>
                //    {

                //        Console.WriteLine(s + u);
                //    }),
                //    result_callback = ((status, exception) =>
                //    {

                //    }),
                //    install_states = (states =>
                //    {
                //        Console.WriteLine(states); ;
                //    })
                //};
                //var information = versionInformations[i1];
                //bedrockCore.InstallVersion(information, information.ID,"./1.appx", callback);

                while (true)
                {
                    var readLine = Console.ReadLine();
                    if (readLine.Contains("1!!!"))
                    {
                        Task.Run((() =>
                        {
                            InstallCallback callback = new InstallCallback()
                            {
                                zipProgress = new Progress<ZipProgress>((progress =>
                                {
                                    Console.WriteLine(progress.ToString());
                                })),
                                registerProcess_percent = ((s, u) =>
                                {

                                    Console.WriteLine(s + u);
                                }),
                                result_callback = ((status, exception) =>
                                {
                                    Console.WriteLine(status);
                                }),
                                install_states = (states =>
                                {
                                    Console.WriteLine(states); ;
                                })
                            };
                            var remove = readLine.Remove(0,4);
                            var changeVersion = bedrockCore.ChangeVersion(remove, callback);
                            Console.WriteLine(changeVersion);
                        }));
                    }

                    if (readLine.Contains("Launch"))
                    {
                        bedrockCore.LaunchGame(VersionType.Release);
                    }
                  
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
