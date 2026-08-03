using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BedrockLauncher.Core;
using BedrockLauncher.Core.SoureGenerate;

public class BuildDatabase
{
	[JsonPropertyName("CreationTime")] public DateTime CreationTime { get; set; }

	[JsonExtensionData] public Dictionary<string, object> ExtensionData { get; set; } = new();

	[JsonIgnore] public IAsyncEnumerable<KeyValuePair<string, BuildInfo>>  Builds => GetBuildsFromExtensionData();

	private async IAsyncEnumerable<KeyValuePair<string, BuildInfo>> GetBuildsFromExtensionData()
	{
		foreach (var (key, value) in ExtensionData)
		{
			if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
			{
				foreach (var jsonProperty in element.EnumerateObject())
				{
					var buildInfo = JsonSerializer.Deserialize(
						jsonProperty.Value.GetRawText(),
						BuildDatabaseContext.Default.BuildInfo);

					if (buildInfo != null)
					{
						yield return new KeyValuePair<string, BuildInfo>(jsonProperty.Name, buildInfo);
					}


					await Task.Yield();
				}

				
			}
		}
	}
}

#region auto Generated from json

public class BuildInfo
{
	public string Key { get; set; } = string.Empty;
	
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
	[JsonPropertyName("Arch")]
	[JsonConverter(typeof(ArchitectureJsonConverter))]
	public Architecture Arch { get; set; }

	[JsonPropertyName("ArchivalStatus")] public int ArchivalStatus { get; set; }

	[JsonPropertyName("OSbuild")] public string OSBuild { get; set; } = string.Empty;

	[JsonPropertyName("MetaData")] public List<string> MetaData { get; set; } = new();

	[JsonPropertyName("MD5")] public string MD5 { get; set; } = string.Empty;
}

#endregion
