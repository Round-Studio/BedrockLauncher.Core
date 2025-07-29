using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BedrockLauncher.Core
{
    public class InstallCallback
    {
        /// <summary>
        /// 取消token
        /// </summary>
        public CancellationToken CancellationToken { get; set; } =   new CancellationToken();

        /// <summary>
        /// 下载进度
        /// </summary>
        public required Progress<DownloadProgress> downloadProgress { get; set; } 
        /// <summary>
        /// 部署进度
        /// </summary>
        public required Action<string, uint> registerProcess_percent { get; set; } 
        /// <summary>
        /// 结果返回
        /// </summary>
        public required Action<AsyncStatus, Exception> result_callback { get; set; }
        /// <summary>
        /// 安装状态
        /// </summary>
        public Action<InstallStates> install_states { get; set; } = new((states => {}));
    }
}
