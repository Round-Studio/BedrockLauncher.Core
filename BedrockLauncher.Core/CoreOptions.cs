namespace BedrockLauncher.Core;

/// <summary>
///     Represents configuration options for core application features.
/// </summary>
public class CoreOptions
{
	/// <summary>
	///     Auto Open Windows DevelopMent
	/// </summary>
	public bool IsAutoOpenDevelopment { get; init; }

	/// <summary>
	///     Auto Complete VisualC++ Runtime (Uwp&Gdk)
	/// </summary>
	public bool IsAutoCompleteVC { get; init; }

	/// <summary>
	///     Gets a value indicating whether MD5 checksum verification is enabled.
	/// </summary>
	public bool IsCheckMD5 { get; init; }
}