using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.FrameworkComplete;
using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Network;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.System;
using static BedrockLauncher.Core.Native.Native;

namespace BedrockLauncher.Core
{
    public class BedrockCore
    {
        public CoreOptions options { get; set; }
        public HttpClient client { get; set; }
        public MultiThreadDownloader downloader { get; set; }


        public BedrockCore()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new Exception("仅支持Windows平台");
            }

            client = new HttpClient();
            downloader = new MultiThreadDownloader();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if (options == null)
            {
                options = new CoreOptions();
            }

            if (options.autoOpenWindowsDevelopment || !GetWindowsDevelopmentState())
            {
                OpenWindowsDevelopment();
            }

            if (options.autoCompleteVC)
            {
                CompleteFrameWorkHelper.CompleteVC();
            }

            if (!Directory.Exists(options.localDir))
            {
                Directory.CreateDirectory(options.localDir);
            }
        }

        /// <summary>
        /// 开启Windows开发者模式
        /// </summary>
        public bool OpenWindowsDevelopment()
        {
            try
            {
                var AppModelUnlock = Registry.LocalMachine.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
                AppModelUnlock.SetValue("AllowDevelopmentWithoutDevLicense", 1);
                return true;
            }
            catch
            {
                return false;
            }

        }
        /// <summary>
        /// 获取Windows开发者模式状态
        /// </summary>
        /// <returns>true为开启，false为关闭</returns>
        public bool GetWindowsDevelopmentState()
        {
            var AppModelUnlock = Registry.LocalMachine.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
            var value = AppModelUnlock.GetValue("AllowDevelopmentWithoutDevLicense", 1);
            if ((int)value == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 安装MC
        /// </summary>
        /// <param name="information">版本信息</param>
        /// <param name="install_dir">安装目录名称</param>
        /// <param name="process_percent">安装进度</param>
        /// <param name="result_callback">结果callback</param>
        /// <returns></returns>
        public bool InstallVersion(VersionInformation information,string install_dir,Action<string,uint> process_percent,Action<AsyncStatus,Exception> result_callback)
        {
            if (!Directory.Exists(options.localDir))
            {
                Directory.CreateDirectory(options.localDir);
            }

            var savePath = Path.Combine(options.localDir, install_dir+".appx");
            var result = downloader.DownloadAsync(VersionHelper.GetUri(client, information.Variations[0].UpdateIds[0].ToString()),
                savePath).Result;
            if (result != true)
            {
                return false;
            }
            
            var destinationDirectoryName = Path.Combine(options.localDir, install_dir);
            ZipFile.ExtractToDirectory(savePath,destinationDirectoryName);
            File.Delete(Path.Combine(destinationDirectoryName, "AppxSignature.p7x"));
            ManifestEditor.EditManifest(destinationDirectoryName);
            TaskCompletionSource<int> task = new TaskCompletionSource<int>();
            Native.Native.RegisterAppxAsync(Path.Combine(destinationDirectoryName, "AppxManifest.xml"),(
                (progress, deploymentProgress) =>
                {
                    process_percent(deploymentProgress.state.ToString(), deploymentProgress.percentage);
                }),((progress, status) =>
               {
                task.SetResult(1);
                if (status == AsyncStatus.Error)
                {
                    result_callback(status, new Exception(progress.GetResults().ErrorText));
                }
                else
                {
                    result_callback(status, null);
                }
               }));
            task.Task.Wait();
            return true;
        }

        /// <summary>
        /// 启动游戏
        /// </summary>
        /// <returns></returns>
        public bool LaunchGame(VersionType type)
        {
           
            var appDiagnosticInfos = AppDiagnosticInfo.RequestInfoForPackageAsync(type switch {
                VersionType.Release => "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                VersionType.Preview => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
                VersionType.Beta => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe"
            }).AsTask().Result;
            if (appDiagnosticInfos.Count!=0)
            {
                appDiagnosticInfos[0].LaunchAsync();
                return true;
            }
            else
            {
                return false;
            }

           
        }
        /// <summary>
        /// 关闭游戏
        /// </summary>
        /// <returns></returns>
        public void StopGame()
        {
            Process[] processes = Process.GetProcessesByName("Minecraft.Windows");
            foreach (var process in processes)
            {
                process.Kill(true);
                Console.WriteLine();
            }
        }
        /// <summary>
        /// 删除游戏
        /// </summary>
        /// <returns></returns>
        public bool RemoveGame(VersionType type)
        {
            try
            {
                var packageManager = new PackageManager();
                var packages = packageManager.FindPackagesForUser("");

                foreach (var package in packages)
                {
                    if (package.Id.FamilyName == type switch
                        {
                            VersionType.Release => "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                            VersionType.Preview => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
                            VersionType.Beta => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe"
                        })
                    {
                        packageManager.RemovePackageAsync(
                            package.Id.FullName,
                            RemovalOptions.PreserveApplicationData).AsTask().Wait();
                    }
                }
                return true;
            }
            catch 
            {
                return false;
            }
            
        }

        private string CheckMD5(string uri)
        {
            return string.Empty;
        }
    
    }
}
