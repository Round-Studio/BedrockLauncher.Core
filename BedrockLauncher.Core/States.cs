using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core
{
    public enum GameStates
    {
        Launching,
        Launched,
        Removing,
        Removed,
    }
    /// <summary>
    /// 安装状态
    /// </summary>
    public enum InstallStates
    {
        downloading,
        downloaded,
        getingDownloadUri,
        gotDownloadUri,
        unzipng,
        unziped,
        registering,
        registered
    }
}
