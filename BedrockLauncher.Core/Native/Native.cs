using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Management.Deployment;
using BedrockLauncher.Core.Network;

namespace BedrockLauncher.Core.Native
{
    public static class Native
    {
        [ComImport, Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
      public interface IApplicationActivationManager
        {
            int ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
                [MarshalAs(UnmanagedType.LPWStr)] string arguments,
                int Options, out uint processId);
        }

        /// <summary>
        /// 异步注册appx 请使用TaskCompletionSource进行等待
        /// </summary>
        /// <param name="appxXmlpath"></param>
        /// <param name="ProgressCallAction"></param>
        /// <param name="completeAction"></param>
         [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PackageManager))]
        public static void RegisterAppxAsync(string appxXmlpath,Action <IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress>,DeploymentProgress> ProgressCallAction, Action <IAsyncOperationWithProgress<DeploymentResult,DeploymentProgress>,AsyncStatus> completeAction, CancellationToken ctx)
        {
            try
            {
                var manager = new PackageManager();
                var asyncOperationWithProgress = manager.RegisterPackageAsync(new Uri(appxXmlpath), null, DeploymentOptions.DevelopmentMode);
                asyncOperationWithProgress.Progress += ((info, progressInfo) => ProgressCallAction(info,progressInfo));
                asyncOperationWithProgress.Completed += ((info, status) => completeAction(info, status));
                if (ctx == null)
                {
                    return;
                }
                asyncOperationWithProgress.AsTask().WaitAsync(ctx);
            }
            catch 
            {
                throw;
            }
        }
        /// <summary>
        /// 异步添加框架appx 请使用TaskCompletionSource进行等待
        /// </summary>
        /// <param name="appxPath"></param>
        /// <param name="ProgressCallAction"></param>
        /// <param name="completeAction"></param>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(PackageManager))]
        public static void addAppxAsync(string appxPath, Action<IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress>, DeploymentProgress> ProgressCallAction, Action<IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress>, AsyncStatus> completeAction)
        {
            try
            {
                var packageManager = new PackageManager();
                var asyncOperationWithProgress = packageManager.AddPackageAsync(new Uri(appxPath),null,DeploymentOptions.ForceApplicationShutdown);
                asyncOperationWithProgress.Progress += ((info, progressInfo) => ProgressCallAction(info, progressInfo));
                asyncOperationWithProgress.Completed += ((info, status) => completeAction(info, status));
            }
            catch
            {
               
                throw;
            }
        }
    }
}
