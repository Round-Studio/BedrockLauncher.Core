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
         

                //InstallCallback callback = new InstallCallback()
                //{
                //    registerProcess_percent = ((s, u) =>
                //    {
                //        Console.WriteLine(u);
                //    }),
                //    result_callback = ((status, exception) =>
                //    {

                //    }),
                //    zipProgress = (new Progress<ZipProgress>((progress =>
                //    {

                //    }))),
                //    downloadProgress = (new Progress<DownloadProgress>((progress =>
                //    {
                //        Console.WriteLine(progress.ProgressPercentage);
                //    }))),
                //    install_states = (states =>
                //    {
                //        Console.WriteLine(states);
                //    })
                //};
                //    var bedrockCore = new BedrockCore();
                //    bedrockCore.Init();
                ////    bedrockCore.InstallVersionByappx("./a.appx","Test",Path.Combine(Directory.GetCurrentDirectory(),"Testa"),callback);
                //    var versionInformations = VersionHelper.GetVersions("https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/raw/main/data.json");
                //    int i = 0;
                //    versionInformations.ForEach((a) =>
                //    {
                //        Console.WriteLine(a.ID + $"[{i}]" + a.Type);
                //        i++;
                //    });

                //    var readLine = Console.ReadLine();
                //    var i1 = int.Parse(readLine);
                //    bedrockCore.Init();
                //    InstallCallback callback = new InstallCallback()
                //    {
                //        registerProcess_percent = ((s, u) =>
                //        {
                //            Console.WriteLine(u);
                //        }),
                //        result_callback = ((status, exception) =>
                //        {
                //            Console.WriteLine(status);
                //        }),
                //        zipProgress = (new Progress<ZipProgress>((progress =>
                //        {
                //            Console.WriteLine(progress.Percentage);
                //        }))),
                //        downloadProgress = (new Progress<DownloadProgress>((progress =>
                //        {
                //            Console.WriteLine(progress.ProgressPercentage);
                //        }))),
                //        install_states = (states =>
                //        {
                //            Console.WriteLine(states);
                //        })
                //    };
                //    string downloadAppx = bedrockCore.DownloadAppx(versionInformations[i1].Variations[0], "./a.appx", callback);
                //    bedrockCore.InstallVersionByappx(downloadAppx,"mc1121",Path.Combine(Directory.GetCurrentDirectory(),"auv"),callback);


            }
    }
}