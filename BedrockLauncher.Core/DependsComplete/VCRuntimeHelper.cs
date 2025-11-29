using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using BedrockLauncher.Core.UwpRegister;
using Windows.Management.Deployment;

namespace BedrockLauncher.Core.DependsComplete;

public static class VCRuntimeHelper
{
	/// <summary>
	/// Provides well-known URIs for downloading Visual C++ Redistributable and related runtime packages for various
	/// Windows platforms.
	/// </summary>
	/// <remarks>The static fields of this struct expose direct download links for different architectures,
	/// including UWP and Win32 variants. These URIs can be used to programmatically retrieve the appropriate
	/// redistributable package for deployment or installation scenarios.</remarks>
	public struct VCUri
	{
		public static string Uwpx64 =
			"https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/3112f116e0cebdf5b1ead2da347f516406e2a365/Microsoft.VCLibs.140.00_14.0.33519.0_x64__8wekyb3d8bbwe.Appx";

		public static string Uwpx86 =
			"https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/f1cead0f80316261fd170c8f54f6cca99f4eaf22/Microsoft.VCLibs.140.00_14.0.33519.0_x86__8wekyb3d8bbwe.Appx";

		public static string Uwparm =
			"https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/106072935eb8232132813cec6c98b979544f69d6/Microsoft.VCLibs.140.00_14.0.33519.0_arm__8wekyb3d8bbwe.Appx";

		public static string Uwparm64 =
			"https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/90f5bd2c05a92f1ed5b60e1a5cc69be1627cff13/Microsoft.VCLibs.140.00_14.0.33519.0_arm64__8wekyb3d8bbwe.Appx";

		public static string Win32x64 =
			"https://gitcode.com/gcw_lJgzYtGB/RecycleObjects/releases/download/VCRuntime140GDK/VC_redist.x64.exe";

		public static string Win32x86 =
			"https://gitcode.com/gcw_lJgzYtGB/RecycleObjects/releases/download/VCRuntime140GDK/VC_redist.x86.exe";

		public static string Win32arm64 =
			"https://gitcode.com/gcw_lJgzYtGB/RecycleObjects/releases/download/VCRuntime140GDK/VC_redist.arm64.exe";

		public static string GameInputRedist =
			"https://raw.gitcode.com/gcw_lJgzYtGB/RecycleObjects/blobs/babbbbf96d352658f85ff0287e64bcd485b5f001/GameInputRedist.msi";
	}
	/// <summary>
	/// Downloads and completes the installation of the Visual C++ Runtime package for the specified architecture and
	/// Minecraft build type asynchronously.
	/// </summary>
	/// <param name="architecture">The target processor architecture for which the Visual C++ Runtime package should be installed.</param>
	/// <param name="installType">The Minecraft build type and version that determines the appropriate Visual C++ Runtime package to download.</param>
	/// <exception cref="BedrockCoreException">Thrown if the Visual C++ Runtime package cannot be downloaded or installed.</exception>
	public static async Task CompleteVCRuntimeAsync([NotNull] Architecture architecture)
	{
		using (var client = new HttpClient())
		{
			try
			{
				async Task<byte[]> DownloadPackageAsync(string uri)
				{
					var async = await client.GetAsync(uri);
					if (async.StatusCode != HttpStatusCode.OK) throw new BedrockCoreNetWorkError("Get VCPackage Error");
					return await async.Content.ReadAsByteArrayAsync();
				}

				var uwpVC = await DownloadPackageAsync(architecture switch
				{
					Architecture.X86 => VCUri.Uwpx86,
					Architecture.X64 => VCUri.Uwpx64,
					Architecture.Arm64 => VCUri.Uwparm64,
					_=>VCUri.Uwpx64
				});
				var gdkVC = await DownloadPackageAsync(architecture switch
				{
					Architecture.X64=>VCUri.Win32x64,
					Architecture.X86=>VCUri.Win32x86,
					Architecture.Arm64=>VCUri.Uwparm64,
					_=>VCUri.Win32x64
				});
				var tempuwp = Path.GetTempFileName() + ".appx";
				var tempgdk = Path.GetTempFileName() + ".exe";
				File.WriteAllBytes(tempuwp, uwpVC);
				File.WriteAllBytes(tempgdk, gdkVC);
				await UwpRegister.UwpRegister.AddAppxAsync(new DeploymentOptionsConfig
				{
					CancellationToken = new CancellationToken(false),
					DeploymentOptions = DeploymentOptions.ForceApplicationShutdown,
					PackagePath = tempuwp
				});
				var startInfo = new ProcessStartInfo
				{
					FileName = tempgdk,
					Arguments = "/install /quiet",
					UseShellExecute = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};
				using (var process = new Process { StartInfo = startInfo })
				{
					process.Start();
				}
			}
			catch
			{
				throw new BedrockCoreException("Get VCPackage Error");
			}
		}
	}
	[DllImport("msi.dll", CharSet = CharSet.Unicode)]
	static extern Int32 MsiInstallProduct(string szPackagePath, string szCommandLine);

	[DllImport("msi.dll", CharSet = CharSet.Unicode)]
	static extern Int32 MsiConfigureProduct(string szProduct, int iInstallLevel, int eInstallState);

	[DllImport("msi.dll", SetLastError = true)]
	static extern int MsiGetProductInfo(string productCode, string property,
		[Out] StringBuilder valueBuf, ref int len);

	/// <summary>
	/// Use Windows Installer API To Install MSI
	/// </summary>
	private static bool InstallUsingMsiApi(string msiPath)
	{
		try
		{
			string commandLine = "ACTION=INSTALL REBOOT=ReallySuppress UILevel=2";

			int result = MsiInstallProduct(msiPath, commandLine);

			return result == 0; // ERROR_SUCCESS
		}
		catch
		{
			return false;
		}
	}
	public static async Task InstallGameInput()
	{
		try
		{
			using (var client = new HttpClient())
			{
				async Task<byte[]> DownloadPackageAsync(string uri)
				{
					var async = await client.GetAsync(uri);
					if (async.StatusCode != HttpStatusCode.OK) throw new BedrockCoreNetWorkError("Get VCPackage Error");
					return await async.Content.ReadAsByteArrayAsync();
				}

				var packages = await DownloadPackageAsync(VCUri.GameInputRedist);
				var fileName = Path.GetTempFileName()+".msi";
				 _ = InstallUsingMsiApi(fileName);
			}
		}
		catch 
		{
			throw new BedrockCoreException("Install GameInputRedist Error");
		}
	}

}