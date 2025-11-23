using System.Text.Json;
using System.Text.Json.Serialization;

namespace BedrockLauncher.Core;

public enum MinecraftBuildTypeVersion
{
	GDK,
	UWP,
	UNKNOWN
}

public enum MinecraftGameTypeVersion
{
	Preview,
	Release,
	Beta,
	Unknown
}

public class MinecraftGameTypeVersionConverter : JsonConverter<MinecraftGameTypeVersion>
{
	public override MinecraftGameTypeVersion Read(ref Utf8JsonReader reader, Type typeToConvert,
		JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var stringValue = reader.GetString();
			return stringValue?.ToLower() switch
			{
				"preview" => MinecraftGameTypeVersion.Preview,
				"release" => MinecraftGameTypeVersion.Release,
				"beta" => MinecraftGameTypeVersion.Beta,
				_ => MinecraftGameTypeVersion.Unknown
			};
		}

		return MinecraftGameTypeVersion.Unknown;
	}

	public override void Write(Utf8JsonWriter writer, MinecraftGameTypeVersion value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString().ToLower());
	}
}

public class MinecraftBuildTypeVersionConverter : JsonConverter<MinecraftBuildTypeVersion>
{
	public override MinecraftBuildTypeVersion Read(ref Utf8JsonReader reader, Type typeToConvert,
		JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var stringValue = reader.GetString();
			return stringValue?.ToUpper() switch
			{
				"UWP" => MinecraftBuildTypeVersion.UWP,
				"GDK" => MinecraftBuildTypeVersion.GDK
			};
		}

		return MinecraftBuildTypeVersion.UNKNOWN;
	}

	public override void Write(Utf8JsonWriter writer, MinecraftBuildTypeVersion value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString().ToUpper());
	}
}