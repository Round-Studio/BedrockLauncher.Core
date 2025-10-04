using BedrockLauncher.Core.JsonHandle;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization;

namespace BedrockLauncher.Core.JsonHandle
{
    [JsonSerializable(typeof(VersionInformation))]
    [JsonSerializable(typeof(List<VersionInformation>))]
    [JsonSerializable(typeof(VersionDetils))]
    [JsonSerializable(typeof(List<VersionDetils>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, JsonElement>))]
    [JsonSerializable(typeof(JsonElement))]
    public partial class VersionJsonContext : JsonSerializerContext
    {

    }
}
namespace BedrockLauncher.Core.JsonHandle
{
    public class VersionInformation
    {
        /// <summary>
        /// 版本类型
        /// </summary>
        [JsonPropertyName("Type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 版本id
        /// </summary>
        [JsonPropertyName("ID")]
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// 发布日期
        /// </summary>
        [JsonPropertyName("Date")]
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// 各系统架构变体信息列表
        /// </summary>
        [JsonPropertyName("Variations")]
        public List<VersionDetils> Variations { get; set; } = new List<VersionDetils>();
    }

    public class VersionDetils
    {
        /// <summary>
        /// 架构
        /// </summary>
        [JsonPropertyName("Arch")]
        public string Arch { get; set; } = string.Empty;

        /// <summary>
        /// 发布状态
        /// </summary>
        [JsonPropertyName("ArchivalStatus")]
        public int ArchivalStatus { get; set; }

        /// <summary>
        /// 游戏适用的最低 Windows 版本构建号
        /// </summary>
        [JsonPropertyName("OSbuild")]
        public string OSbuild { get; set; } = string.Empty;

        /// <summary>
        /// 关联的Update ID
        /// </summary>
        [JsonPropertyName("UpdateIds")]
        public List<string> UpdateIds { get; set; } = new List<string>();

        /// <summary>
        /// 该版本对应文件的 MD5 校验值
        /// </summary>
        [JsonPropertyName("MD5")]
        public string MD5 { get; set; } = string.Empty;
    }
}