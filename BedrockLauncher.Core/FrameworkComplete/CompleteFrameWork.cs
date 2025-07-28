using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Management.Deployment;
using BedrockLauncher.Core.Native;
namespace BedrockLauncher.Core.FrameworkComplete
{
    public static class CompleteFrameWorkHelper
    {
        public struct VCUri
        {
            public static string x64 = "https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/3112f116e0cebdf5b1ead2da347f516406e2a365/Microsoft.VCLibs.140.00_14.0.33519.0_x64__8wekyb3d8bbwe.Appx";
            public static string x86 = "https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/f1cead0f80316261fd170c8f54f6cca99f4eaf22/Microsoft.VCLibs.140.00_14.0.33519.0_x86__8wekyb3d8bbwe.Appx";
            public static string arm = "https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/106072935eb8232132813cec6c98b979544f69d6/Microsoft.VCLibs.140.00_14.0.33519.0_arm__8wekyb3d8bbwe.Appx";
            public static string arm64 = "https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/90f5bd2c05a92f1ed5b60e1a5cc69be1627cff13/Microsoft.VCLibs.140.00_14.0.33519.0_arm64__8wekyb3d8bbwe.Appx";
        }
        /// <summary>
        /// 自动补全vc如果存在则不安装
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void CompleteVC()
        {
            var multiThreadDownloader = new ImprovedFlexibleMultiThreadDownloader();
            var packageManager = new PackageManager();

            void AddAppx()
            {
                TaskCompletionSource<int> task = new TaskCompletionSource<int>();
                Native.Native.addAppxAsync(Path.Combine(Directory.GetCurrentDirectory(), "VCUwp.appx"), ((progress, deploymentProgress) =>
                {

                }), ((progress, status) =>
                {
                    task.SetResult(1);
                    if (status == AsyncStatus.Error)
                    {
                        throw new Exception(progress.GetResults().ErrorText);
                    }
                    Console.WriteLine(status);
                }));

                task.Task.Wait();
            }

            void DownLoad(string uri)
            {
                try
                {
                    multiThreadDownloader.DownloadAsync(uri, ".\\VCUwp.appx").Wait();
                    multiThreadDownloader.Dispose();
                    AddAppx();
                    File.Delete(".\\VCUwp.appx");
                }
                catch
                {
                    throw new Exception("下载错误");
                }
               
            }

            bool hasVC = false;
            var packages = packageManager.FindPackages();
            foreach (var package in packages)
            {
                
                if (package.Id.Name.StartsWith("Microsoft.VCLibs.140.00"))
                {
                    hasVC = true;
                    continue;
                }
            }

            if (hasVC == false)
            {
                var osArchitecture = RuntimeInformation.OSArchitecture;
                switch (osArchitecture)
                {
                    case Architecture.Arm:
                        DownLoad(VCUri.arm);
                        break;
                    case Architecture.Arm64:
                        DownLoad(VCUri.arm64);
                        break;
                    case Architecture.X64:
                        DownLoad(VCUri.x64);
                        break;
                    case Architecture.X86:
                        DownLoad(VCUri.x86);
                        break;
                }
            }
           
        }
        
    }
}
