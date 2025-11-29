using System.Runtime.InteropServices;
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
				_ => MinecraftGameTypeVersion.Release
			};
		}

		return MinecraftGameTypeVersion.Release;
	}

	public override void Write(Utf8JsonWriter writer, MinecraftGameTypeVersion value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString().ToLower());
	}
}
public class ArchitectureJsonConverter : JsonConverter<Architecture>
{
	public override Architecture Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return default;

		if (reader.TokenType == JsonTokenType.String)
		{
			string? stringValue = reader.GetString();
			if (string.IsNullOrEmpty(stringValue))
				return 0;

			// 处理各种可能的字符串表示形式
			return stringValue.ToLowerInvariant() switch
			{
				"x64" or "x86_64" or "amd64" => Architecture.X64,
				"x86" or "i386" or "ia32" => Architecture.X86,
				"arm" or "arm32" => Architecture.Arm,
				"arm64" or "aarch64" => Architecture.Arm64,
				"wasm" or "webassembly" => Architecture.Wasm,
				"s390x" => Architecture.S390x,
				"loongarch64" => Architecture.LoongArch64,
				"armv6" => Architecture.Armv6,
				"ppc64le" => Architecture.Ppc64le,
			};
		}

		if (reader.TokenType == JsonTokenType.Number)
		{
			return (Architecture)reader.GetInt32();
		}

		throw new JsonException($"Cant covert this token. TokenType: {reader.TokenType}");
	}

	public override void Write(Utf8JsonWriter writer, Architecture value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString().ToLowerInvariant());
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