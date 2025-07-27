using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedrockLauncher.Core.JsonHandle
{
    public class VersionInformation
    {
    /// <summary>
    /// 版本类型
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// 版本id
    /// </summary>
    public string ID { get; set; }
    /// <summary>
    /// 发布日期
    /// </summary>
    public string Date { get; set; }
    /// <summary>
    /// 各系统架构变体信息列表
    /// </summary>
    public List<VersionDetils> Detils { get; set; }
    }
    public class VersionDetils
    {
        /// <summary>
        /// 架构
        /// </summary>
        public string Arch { get; set; }
        /// <summary>
        /// 发布状态
        /// </summary>
        public int ArchivalStatus { get; set; }
        /// <summary>
        /// 游戏适用的最低 Windows 版本构建号
        /// </summary>
        public string OSbuild { get; set; }
        /// <summary>
        /// 关联的Update ID
        /// </summary>
        public List<string> UpdateIds { get; set; }
        /// <summary>
        /// 该版本对应文件的 MD5 校验值
        /// </summary>
        public string MD5 { get; set; }
    }
}
