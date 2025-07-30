using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core
{
    public class GameBackGroundEditer
    {
        /// <summary>
        /// 文件相对位置
        /// </summary>
        public required string file;
        /// <summary>
        /// 背景颜色Hex16进制
        /// </summary>
        public required string color;
        /// <summary>
        /// 是否启用该功能
        /// </summary>
        public required bool isOpen;
    }
}
