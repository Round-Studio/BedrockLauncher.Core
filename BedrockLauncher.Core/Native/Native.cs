using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace BedrockLauncher.Core.Native
{
    public static class Native
    {

        public static void RegisterAppx(string appxXmlpath,Action <IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress>,DeploymentProgress> ProgressCallAction, Action <IAsyncOperationWithProgress<DeploymentResult,DeploymentProgress>,AsyncStatus> completeAction)
        {
            try
            {
                var manager = new PackageManager();
                var asyncOperationWithProgress = manager.RegisterPackageAsync(new Uri(appxXmlpath), null, DeploymentOptions.DevelopmentMode);
                asyncOperationWithProgress.Progress += ((info, progressInfo) => ProgressCallAction(info,progressInfo));
                asyncOperationWithProgress.Completed += ((info, status) => completeAction(info, status));
            }
            catch 
            {
                throw;
            }
        }
    }
}
