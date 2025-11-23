using Microsoft.Win32;

namespace BedrockLauncher.Core;

public class BedrockCore
{
	private Lazy<MultiThreadDownloader> MultiThreadDownloader = new();

	public BedrockCore()
	{
		if (Environment.OSVersion.Version.Build < 19041)
			throw new BedrockCoreException("Not Support Windows Version (<19041)");
		Options = new CoreOptions();
	}

	public BedrockCore(CoreOptions options)
	{
		if (Environment.OSVersion.Version.Build < 19041)
			throw new BedrockCoreException("Not Support Windows Version (<19041)");
		Options = options;
	}

	/// <summary>
	///     Gets or sets the configuration options for the core system.
	/// </summary>
	public CoreOptions Options { get; set; }

	/// <summary>
	///     Init BedrockCore
	/// </summary>
	public Task InitAsync()
	{
		var task = Task.Run(() => { });
		return task;
	}

	/// <summary>
	///     Get Windows Development state
	/// </summary>
	/// <returns></returns>
	/// <exception cref="BedrockCoreException"></exception>
	public bool GetWindowsDevelopmentState()
	{
		try
		{
			var AppModelUnlock = Registry.LocalMachine.OpenSubKey(
				"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
			var value = AppModelUnlock.GetValue("AllowDevelopmentWithoutDevLicense", 1);
			if ((int)value == 0) return false;
			return true;
		}
		catch
		{
			throw new BedrockCoreException("Can't Get Development state");
		}
	}

	/// <summary>
	///     Enables Windows Developer Mode by modifying the system registry to allow development without a developer license.
	/// </summary>
	/// <remarks>
	///     Administrator privileges are required to modify the system registry. Enabling Developer Mode
	///     allows the installation and testing of apps without a developer license. Use with caution, as modifying the
	///     registry can affect system stability and security.
	/// </remarks>
	/// <returns>true if Developer Mode is successfully enabled; otherwise, false.</returns>
	/// <exception cref="Exception">
	///     Thrown if the operation fails to enable Developer Mode, such as due to insufficient permissions or registry access
	///     errors.
	/// </exception>
	public bool OpenWindowsDevelopment()
	{
		try
		{
			var AppModelUnlock = Registry.LocalMachine.OpenSubKey(
				"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AppModelUnlock", true);
			AppModelUnlock.SetValue("AllowDevelopmentWithoutDevLicense", 1);
			return true;
		}
		catch
		{
			throw new BedrockCoreException("Can't Open Deveopment Successfully");
		}
	}
}