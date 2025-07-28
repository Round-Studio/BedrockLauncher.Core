using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.CoreOption
{
    public class CoreOptions
    {
        /// <summary>
        /// 是否自动打开Windows开发者模式
        /// </summary>
        public bool autoOpenWindowsDevelopment { get; set; }
        /// <summary>
        /// 安装文件夹
        /// </summary>
        public string localDir { get; set; } = "./";
        /// <summary>
        /// 自动补全VC UWP运行库
        /// </summary>
        public bool autoCompleteVC { get; set; }
        
    }
}
