using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.FrameworkComplete;
using BedrockLauncher.Core.JsonHandle;
using BedrockLauncher.Core.Network;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VersionHelper))]
        public void Init()
        {
            if (Options == null)
            {
                Options = new CoreOptions() { };
            }

            if (Options.autoOpenWindowsDevelopment || !GetWindowsDevelopmentState())
            {
                OpenWindowsDevelopment();
            }

            if (Options.autoCompleteVC)
            {
                CompleteFrameWorkHelper.CompleteVC();
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
                throw new Exception("无法正常开启开发者模式");
            }
        }

        /// <summary>
        /// 获取Windows开发者模式状态
        /// </summary>
        /// <returns>true为开启，false为关闭</returns>
        public bool GetWindowsDevelopmentState()
        {
            try
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
            catch
            {
                throw new Exception("无法正常获取Windows开发者状态");
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VersionHelper))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Registry))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ImprovedFlexibleMultiThreadDownloader))]
        public string DownloadAppx(VersionDetils information, string appx_dir, InstallCallback callback)
        {
            try
            {
                callback.install_states(InstallStates.getingDownloadUri);
                var uri = VersionHelper.GetUri(information.UpdateIds[0].ToString());
                callback.install_states(InstallStates.gotDownloadUri);
                lock (Downloader)
                {
                    callback.install_states(InstallStates.downloading);
                    var result = Downloader.DownloadAsync(
                        uri,
                        appx_dir, callback.downloadProgress, callback.CancellationToken).Result;
                    callback.install_states(InstallStates.downloaded);
                    if (result != true)
                    {
                        return string.Empty;
                    }
                }

                return appx_dir;
            }
            catch (Exception e)
            {
                throw;
            }

            return string.Empty;
        }

        /// <summary>
        /// 安装MC
        /// </summary>
        /// <param name="information">版本信息</param>
        /// <param name="install_dir">安装目录</param>
        /// <param name="appx">appx存储</param>
        /// <param name="callback">回调</param>
        /// <param name="gameBackGround">游戏启动屏幕修改(如果你不知道你在干什么请勿填写)</param>
        /// <returns></returns>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VersionHelper))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Registry))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ImprovedFlexibleMultiThreadDownloader))]
        public void InstallVersion(VersionDetils information, VersionType type, string appx, string Gamename,
            string install_dir, InstallCallback callback, GameBackGroundEditer gameBackGround = null)
        {
            try
            {
                var downloadAppx = DownloadAppx(information, appx, callback);
                if (callback.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                //    RemoveGame(type);
                InstallVersionByappx(downloadAppx, Gamename, install_dir, callback, gameBackGround);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VersionHelper))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Registry))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ImprovedFlexibleMultiThreadDownloader))]
        public void InstallVersionByappx(string appx, string Gamename, string install_dir, InstallCallback callback,
            GameBackGroundEditer gameBackGround = null)
        {

            if (!Directory.Exists(install_dir))
            {
                Directory.CreateDirectory(install_dir);
            }

            callback.install_states(InstallStates.unzipng);
            ZipExtractor.ExtractWithProgress(appx, install_dir, callback.zipProgress);
            callback.install_states(InstallStates.unziped);
            File.Delete(Path.Combine(install_dir, "AppxSignature.p7x"));
            ManifestEditor.EditManifest(install_dir, Gamename, gameBackGround);
            callback.install_states(InstallStates.registering);
            var native = new Native.Native();
            var task = DeploymentProgressWrapper(
                new PackageManager().RegisterPackageAsync(new Uri(Path.Combine(install_dir, "AppxManifest.xml")), null,
                    DeploymentOptions.DevelopmentMode | DeploymentOptions.ForceUpdateFromAnyVersion), callback);
        }

        /// <summary>
        /// 更换版本
        /// </summary>
        /// <param name="Version">游戏本体路径文件夹(绝对路径！！)，此文件夹中包含AppxManifest.xml</param>
        /// <returns></returns>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Registry))]
        public bool ChangeVersion(string Version, InstallCallback callback)
        {
            if (Version == string.Empty)
            {
                return false;
            }

            var xml = Path.Combine(Version, "AppxManifest.xml");
            var source = new CancellationTokenSource();
            if (File.Exists(xml))
            {

                callback.install_states(InstallStates.registering);
                var task = DeploymentProgressWrapper(
                    new PackageManager().RegisterPackageAsync(new Uri(xml), null,
                        DeploymentOptions.DevelopmentMode | DeploymentOptions.ForceUpdateFromAnyVersion), callback);
                if (task.Exception == null)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task DeploymentProgressWrapper(
            IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> t, InstallCallback callback)
        {
            TaskCompletionSource<int> src = new TaskCompletionSource<int>();
            t.Progress += (v, p) =>
            {
                Task.Run((() =>
                {
                    callback.registerProcess_percent(p.state.ToString(), p.percentage);
                    Debug.WriteLine("Deployment progress: " + p.state + " " + p.percentage + "%");
                }));
            };
            t.Completed += (v, p) =>
            {
                if (p == AsyncStatus.Error)
                {
                    Task.Run((() => { callback.result_callback(p, new Exception(v.GetResults().ErrorText)); }));
                    Debug.WriteLine("Deployment failed: " + v.GetResults().ErrorText);
                    src.SetException(new Exception("Deployment failed: " + v.GetResults().ErrorText));
                }
                else
                {
                    Task.Run((() =>
                    {
                        callback.install_states(InstallStates.registered);
                        callback.result_callback(p, null);
                    }));
                    src.SetResult(1);
                }
            };
            await src.Task;
        }

        /// <summary>
        /// 启动游戏 如果你要切换你安装的游戏请在调用次函数前调用ChangeVersion函数
        /// </summary>
        /// <returns></returns>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Registry))]
        public bool LaunchGame(VersionType type)
        {
            var appDiagnosticInfos = AppDiagnosticInfo.RequestInfoForPackageAsync(type switch
            {
                VersionType.Release => "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
                VersionType.Preview => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
                VersionType.Beta => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe"
            }).AsTask().Result;
            if (appDiagnosticInfos.Count != 0)
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
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PackageManager))]
        public void RemoveGame(VersionType type)
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
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PackageManager))]
        public void RemoveGameClearly(VersionType type)
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
                        package.Id.FullName).AsTask().Wait();
                }
            }
        }

    }
}