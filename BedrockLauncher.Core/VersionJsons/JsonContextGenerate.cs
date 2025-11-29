using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace BedrockLauncher.Core.SoureGenerate;

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	WriteIndented = true)]
[JsonSerializable(typeof(BuildDatabase))]
[JsonSerializable(typeof(BuildInfo))]
[JsonSerializable(typeof(List<Variation>))]
[JsonSerializable(typeof(Variation))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(MinecraftBuildTypeVersion))]
[JsonSerializable(typeof(MinecraftGameTypeVersion))]
[JsonSerializable(typeof(Dictionary<string, BuildInfo>))]
[JsonSerializable(typeof(Architecture))]
public partial class BuildDatabaseContext : JsonSerializerContext
{
}