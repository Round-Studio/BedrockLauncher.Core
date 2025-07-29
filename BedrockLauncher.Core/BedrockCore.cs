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
using BedrockLauncher.Core.Native;
using static BedrockLauncher.Core.Native.Native;

namespace BedrockLauncher.Core
{
    public class BedrockCore
    {
        public CoreOptions Options { get; set; }
        public ImprovedFlexibleMultiThreadDownloader Downloader { get; set; }


        public BedrockCore()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new Exception("仅支持Windows平台");
            }
            Downloader = new ImprovedFlexibleMultiThreadDownloader();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            if (Options == null)
            {
                Options = new CoreOptions();
            }

            if (Options.autoOpenWindowsDevelopment || !GetWindowsDevelopmentState())
            {
                OpenWindowsDevelopment();
            }

            if (Options.autoCompleteVC)
            {
                CompleteFrameWorkHelper.CompleteVC();
            }

            if (!Directory.Exists(Options.localDir))
            {
                Directory.CreateDirectory(Options.localDir);
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
        /// <param name="downloadProgress">下载进度</param>
        /// <param name="install_dir">安装目录名称</param>
        /// <param name="process_percent">安装进度</param>
        /// <param name="result_callback">结果callback</param>
        /// <returns></returns>
        public bool InstallVersion(VersionInformation information,string install_dir,InstallCallback callback)
        {
           
            var savePath = Path.Combine(Options.localDir, install_dir + ".appx");
            try
            {

                if (!Directory.Exists(Options.localDir))
                {
                    Directory.CreateDirectory(Options.localDir);
                }

                callback.install_states(InstallStates.getingDownloadUri);
                var uri = VersionHelper.GetUri(information.Variations[0].UpdateIds[0].ToString());
                callback.install_states(InstallStates.gotDownloadUri);
                lock (Downloader)
                {
                    callback.install_states(InstallStates.downloading);
                    var result = Downloader.DownloadAsync(
                        uri,
                        savePath, callback.downloadProgress,callback.CancellationToken).Result;
                    callback.install_states(InstallStates.downloaded);
                    if (result != true)
                    {
                        return false;
                    }
                }


                var destinationDirectoryName = Path.Combine(Options.localDir, install_dir);
                callback.install_states(InstallStates.unzipng);
                ZipExtractor.ExtractWithProgress(savePath,destinationDirectoryName,callback.zipProgress);
                //ZipFile.ExtractToDirectory(savePath, destinationDirectoryName,true);
                callback.install_states(InstallStates.unziped);
                File.Delete(Path.Combine(destinationDirectoryName, "AppxSignature.p7x"));

                ManifestEditor.EditManifest(destinationDirectoryName);

                TaskCompletionSource<int> task = new TaskCompletionSource<int>();
                callback.install_states(InstallStates.registering);
                Native.Native.RegisterAppxAsync(Path.Combine(destinationDirectoryName, "AppxManifest.xml"), (
                    (progress, deploymentProgress) =>
                    {
                      callback.registerProcess_percent(deploymentProgress.state.ToString(), deploymentProgress.percentage);
                    }), ((progress, status) =>
                {
                    task.SetResult(1);
                    if (status == AsyncStatus.Error)
                    {
                        callback.result_callback(status, new Exception(progress.GetResults().ErrorText));
                    }
                    else
                    {
                        callback.result_callback(status, null);
                    }
                }));
                task.Task.Wait();
                callback.install_states(InstallStates.registered);
                return true;
            }
            catch (Exception ex)
            {
               throw;
            }
            finally
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            }
           
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
