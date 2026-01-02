namespace BedrockLauncher.Core.CoreOption;

/// <summary>
///     Represents configuration options for core application features.
/// </summary>
public class CoreOptions
{
	/// <summary>
	///     Auto Open Windows DevelopMent
	/// </summary>
	public bool IsAutoOpenDevelopment { get; set; }

	/// <summary>
	///     Auto Complete VisualC++ Runtime (Uwp&Gdk)
	/// </summary>
	public bool IsAutoCompleteVC { get; set; }

	/// <summary>
	///     Gets a value indicating whether MD5 checksum verification is enabled.
	/// </summary>
	public bool IsCheckMD5 { get; set; }
	/// <summary>
	/// Gets or sets a value indicating whether the input is treated as game input for auto-complete operations.
	/// </summary>
	public bool IsAutoCompleteGameInput { get; set; }
}