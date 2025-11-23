using System.Text.Json;
using System.Text.Json.Serialization;
using BedrockLauncher.Core;
using BedrockLauncher.Core.SoureGenerate;

public class BuildDatabase
{
	[JsonPropertyName("CreationTime")] public DateTime CreationTime { get; set; }

	[JsonExtensionData] public Dictionary<string, object> ExtensionData { get; set; } = new();

	[JsonIgnore] public Dictionary<string, BuildInfo> Builds => GetBuildsFromExtensionData();

	private Dictionary<string, BuildInfo> GetBuildsFromExtensionData()
	{
		var result = new Dictionary<string, BuildInfo>();

		foreach (var (key, value) in ExtensionData)
			if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
			{
				var buildInfo = JsonSerializer.Deserialize(
					element.GetRawText(),
					BuildDatabaseContext.Default.DictionaryStringBuildInfo);
				if (buildInfo != null) result = buildInfo;
			}

		return result;
	}
}

#region auto Generated from json

public class BuildInfo
{
	[JsonPropertyName("Type")]
	[JsonConverter(typeof(MinecraftGameTypeVersionConverter))]
	public MinecraftGameTypeVersion Type { get; set; }

	[JsonPropertyName("BuildType")]
	[JsonConverter(typeof(MinecraftBuildTypeVersionConverter))]
	public MinecraftBuildTypeVersion BuildType { get; set; }

	[JsonPropertyName("ID")] public string ID { get; set; } = string.Empty;

	[JsonPropertyName("Date")] public string Date { get; set; } = string.Empty;

	[JsonPropertyName("Variations")] public List<Variation> Variations { get; set; } = new();
}

public class Variation
{
	[JsonPropertyName("Arch")] public string Arch { get; set; } = string.Empty;

	[JsonPropertyName("ArchivalStatus")] public int ArchivalStatus { get; set; }

	[JsonPropertyName("OSbuild")] public string OSBuild { get; set; } = string.Empty;

	[JsonPropertyName("MetaData")] public List<string> MetaData { get; set; } = new();

	[JsonPropertyName("MD5")] public string MD5 { get; set; } = string.Empty;
}

#endregion