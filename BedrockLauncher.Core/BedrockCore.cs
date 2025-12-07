#pragma warning disable CS8524
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.DependsComplete;
using BedrockLauncher.Core.GdkDecode;
using BedrockLauncher.Core.Utils;
using BedrockLauncher.Core.UwpRegister;
using Microsoft.Win32;
using Windows.Management.Deployment;
using Windows.System;

namespace BedrockLauncher.Core;

public class BedrockCore
{
	public Lazy<MultiThreadDownloader> MultiThreadDownloader = new();

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
	public CoreOptions Options { get; set; }  = new CoreOptions();

	/// <summary>
	///     Initializes the BedrockCore by checking and enabling development mode and completing VC runtime if configured.
	///     This method runs initialization tasks in a background thread.
	/// </summary>
	public async Task InitAsync()
	{
		await Task.Run((() =>
		{
			if (Options.IsAutoOpenDevelopment)
			{
				if (!GetWindowsDevelopmentState())
				{
					OpenWindowsDevelopment();
				}
			}

			if (Options.IsAutoCompleteVC)
			{
				var (item1, item2) = IsHasVCRuntime(RuntimeInformation.OSArchitecture);
				if (!item1 || !item2)
				{
					VCRuntimeHelper.CompleteVCRuntimeAsync(RuntimeInformation.OSArchitecture).Wait();
				}
			}
		}));
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
			var value = AppModelUnlock?.GetValue("AllowDevelopmentWithoutDevLicense", 1);
			if (value == null)
				return false;
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
			AppModelUnlock?.SetValue("AllowDevelopmentWithoutDevLicense", 1);
			return true;
		}
		catch
		{
			throw new BedrockCoreException("Can't Open Deveopment Successfully");
		}
	}
	/// <summary>
	/// Check devices' vc runtime
	/// </summary>
	/// <returns>The first value is for uwp,second is win32.True if the required VC runtime is installed, otherwise false</returns>
	public (bool, bool) IsHasVCRuntime(Architecture arch)
	{
		try
		{
			bool CheckVersion(string[] archli)
			{
				foreach (var s in archli)
				{
#pragma warning disable CS8600 
					using (RegistryKey key = Registry.LocalMachine.OpenSubKey(s))
					{
						if (key != null)
						{
							return true;
						}
					}
#pragma warning restore CS8600 
				}

				return false;
			}
			bool isHasVCwin32 = false;
			bool isHasVCUwp = false;
			string[] registryPaths = arch switch
			{
				Architecture.X64 => new[] { @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64", @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" },
				Architecture.X86 => new[] { @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86", @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86" },
				Architecture.Arm64 => new[] { @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\arm64", @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\arm64" },
				_ => new[] { @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64", @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" }
			};
			isHasVCwin32 = CheckVersion(registryPaths);
			var packageManager = new PackageManager();
			var packages = packageManager.FindPackages();

			var vcRuntimePackages = packages.Where(p =>
				p.Id.Name.Contains("Microsoft.VCLibs.140")
			);
			if (vcRuntimePackages.Count() != 0)
			{
				isHasVCUwp = true;
			}
			return (isHasVCUwp, isHasVCwin32);
		}
		catch
		{
			return (false, false);
		}
	}
	/// <summary>
	/// Installs a Minecraft game package (either GDK or UWP version) to the specified destination folder
	/// </summary>
	/// <param name="options">The local game package installation options containing file path, destination, and progress callbacks</param>
	/// <returns>An InstallResult object containing the deployment result information</returns>
	public async Task<InstallResult?> InstallPackageAsync(LocalGamePackageOptions options)
	{
		var installResult = new InstallResult();
		Directory.CreateDirectory(options.InstallDstFolder);
		if (options.Type == MinecraftBuildTypeVersion.GDK)
		{
			await Task.Run((async () =>
			{
				CikKey cik = new CikKey(options.GameTypeVersion switch
				{
					MinecraftGameTypeVersion.Release => _DEFINE_REF2.rel,
					MinecraftGameTypeVersion.Preview => _DEFINE_REF2.pre,
					MinecraftGameTypeVersion.Beta => _DEFINE_REF2.pre,
					_=>null
				});
				var msiXvdDecoder = new MsiXVDDecoder(cik);
				var msiXvdStream = new MsiXVDStream(options.FileFullPath);
				msiXvdStream.Parse();
				options.InstallStates?.Report(InstallStates.Extracting);
				await msiXvdStream.ExtractTaskAsync(Path.GetFullPath(options.InstallDstFolder), msiXvdDecoder,
					options.ExtractionProgress, options.CancellationToken.GetValueOrDefault());
				options.InstallStates?.Report(InstallStates.Extracted);

			}));
			return installResult;
		}

		if (options.Type == MinecraftBuildTypeVersion.UWP)
		{
			options.InstallStates?.Report(InstallStates.Extracting);

			await ZipExtractor.ExtractWithProgressAsync(options.FileFullPath, options.InstallDstFolder,
			   progress: options.ExtractionProgress, options.CancellationToken.GetValueOrDefault());
			File.Delete(Path.Combine(options.InstallDstFolder, "AppxSignature.p7x"));
			options.InstallStates?.Report(InstallStates.Extracted);
			bool is_installed = UwpRegister.UwpRegister.IsPackageInstalled(options.GameTypeVersion switch
			{
				MinecraftGameTypeVersion.Beta => "Microsoft.MinecraftWindowsBeta",
				MinecraftGameTypeVersion.Release => "Microsoft.MinecraftUWP",
				MinecraftGameTypeVersion.Preview => "Microsoft.MinecraftWindowsBeta"
			});
			await ManifestEditor.EditManifest(options.InstallDstFolder, options.GameName ?? TimeBasedVersion.GetVersion(), options.BackGroundConfig);

			var config = new DeploymentOptionsConfig();
			config.PackagePath = Path.Combine(options.InstallDstFolder, "AppxManifest.xml");
			config.CancellationToken = options.CancellationToken.GetValueOrDefault();
			config.Timeout = new TimeSpan(0, 3, 0);
			config.DeploymentOptions = is_installed ? DeploymentOptions.DevelopmentMode | DeploymentOptions.ForceUpdateFromAnyVersion
				: DeploymentOptions.DevelopmentMode;
			config.ProgressCallback = options.DeployProgress;

			options.InstallStates?.Report(InstallStates.Registering);
			var result = await UwpRegister.UwpRegister.RegisterAppxAsync(config);
			options.InstallStates?.Report(InstallStates.Registered);
			installResult.DeploymentResult = result;
			return installResult;
		}
		return null;
	}
	/// <summary>
	/// Starts the Minecraft game process based on the specified launch options
	/// </summary>
	/// <param name="options">The launch options containing game folder, arguments, and build type</param>
	/// <returns>The Process object representing the launched game instance</returns>
	public async Task<Process> StartGameAsync(LaunchOptions options)
	{
		var process = new Process();
		if (options.MinecraftBuildType == MinecraftBuildTypeVersion.GDK)
		{
			options.Progress?.Report(LaunchState.Launching);
			var info = new ProcessStartInfo();
			info.FileName = Path.Combine(options.GameFolder, "Minecraft.Windows.exe");
			info.Arguments = options.LaunchArgs;
			info.UseShellExecute = true;
			info.CreateNoWindow = true;
			process.StartInfo = info;
			process.Start();
			options.Progress?.Report(LaunchState.Launched);
		}

		if (options.MinecraftBuildType == MinecraftBuildTypeVersion.UWP)
		{
			string manifest = Path.Combine(options.GameFolder, "AppxManifest.xml");
			if (!File.Exists(manifest))
			{
				throw new IOException("File doesn't exist");
			}
			PackageManager packageManager = new PackageManager();
			bool twice_launch = false;
			foreach (var package in packageManager.FindPackages())
			{
				if (package.Id.Name.Equals(options.GameType switch
				    {
					    MinecraftGameTypeVersion.Beta => "Microsoft.MinecraftWindowsBeta",
					    MinecraftGameTypeVersion.Release => "Microsoft.MinecraftUWP",
					    MinecraftGameTypeVersion.Preview => "Microsoft.MinecraftWindowsBeta"
				    }, StringComparison.OrdinalIgnoreCase))
				{
					if (Path.GetFullPath(package.InstalledPath) == Path.GetFullPath(options.GameFolder))
					{
						twice_launch = true;
					}
				}
			}
			bool is_installed = UwpRegister.UwpRegister.IsPackageInstalled(options.GameType switch
			{
				MinecraftGameTypeVersion.Beta => "Microsoft.MinecraftWindowsBeta",
				MinecraftGameTypeVersion.Release => "Microsoft.MinecraftUWP",
				MinecraftGameTypeVersion.Preview => "Microsoft.MinecraftWindowsBeta"
			});
			var config = new DeploymentOptionsConfig();
			options.Progress?.Report(LaunchState.Registering);
			config.CancellationToken = options.CancellationToken.GetValueOrDefault();
			config.PackagePath = manifest;
			config.DeploymentOptions = is_installed
				? DeploymentOptions.DevelopmentMode | DeploymentOptions.ForceUpdateFromAnyVersion
				: DeploymentOptions.DevelopmentMode;
			config.ProgressCallback = options.RegisterProgress;
			if (!twice_launch && is_installed || !is_installed)
			{
				var appxAsync = await UwpRegister.UwpRegister.RegisterAppxAsync(config);
				options.Progress?.Report(LaunchState.Registered);
				if (appxAsync.IsRegistered == false)
				{
					throw new Exception(appxAsync.ErrorText);
				}
			}

			var appDiagnosticInfos = AppDiagnosticInfo.RequestInfoForPackageAsync(options.GameType switch
			{
				MinecraftGameTypeVersion.Release => "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
				MinecraftGameTypeVersion.Preview => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
				MinecraftGameTypeVersion.Beta => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe"
			}).AsTask().Result;
			if (appDiagnosticInfos.Count != 0)
			{
				 await appDiagnosticInfos[0].LaunchAsync();
				 
				 Process[] processes = Process.GetProcessesByName("Minecraft.Windows");
				 processes.OrderBy(p => p.StartTime);
				 process = processes.Last();
			}
		}
		return process;
	}
	/// <summary>
	/// Downloads and validates a game package based on the provided online package options
	/// </summary>
	/// <param name="gamePackage">The online package options containing build info, architecture, and download settings</param>
	/// <returns>The build type version of the downloaded game package</returns>
	/// <exception cref="BedrockCoreException">Thrown when the specified architecture version is not found</exception>
	/// <exception cref="BedrockCoreNoAvailbaleVersionUri">Thrown when no available download URI is found</exception>
	/// <exception cref="WebException">Thrown when the downloaded file fails MD5 validation</exception>
	public async Task<MinecraftBuildTypeVersion> GetGamePackage(GameOnlinePackageOptions gamePackage)
	{
		Architecture devicesArch = gamePackage.Architecture.HasValue ? gamePackage.Architecture.Value : RuntimeInformation.OSArchitecture;

		var find = gamePackage.BuildInfo.Variations.Find((variation => variation.Arch == devicesArch));
		if (find == null)
			throw new BedrockCoreException($"Unable to find {devicesArch} Version");
		if (find.MetaData.Count == 0)
			throw new BedrockCoreNoAvailbaleVersionUri("There is no available Uri to download");
		await MultiThreadDownloader.Value.DownloadFileAsync(
			await GetPackageUri(find.MetaData.Last()),
			gamePackage.SaveFilePath,
			gamePackage.DownloadThread.HasValue ? gamePackage.DownloadThread.Value : 4,
			gamePackage.DownloadProgress ?? new Progress<DownloadProgress>(),
			gamePackage.CancellationToken.HasValue ? gamePackage.CancellationToken.Value : default(CancellationToken),
			gamePackage.MaxRetryTimes.HasValue ? gamePackage.MaxRetryTimes.Value : 3
			);
		if (Options.IsCheckMD5)
		{
			var fileMd5 = await ComputeFileMD5.ComputeFileMD5Async(gamePackage.SaveFilePath);
			if (fileMd5 != find.MD5)
			{
				throw new WebException("Download file failed because of md5 mismatch");
			}
		}
		return gamePackage.BuildInfo.BuildType;
	}
	/// <summary>
	/// Remove Uwp Minecraft Game
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public async Task<DeploymentResult?> RemoveUWPGameAsync(MinecraftGameTypeVersion type)
	{

		var packageManager = new PackageManager();
		var packages = packageManager.FindPackagesForUser("");

		foreach (var package in packages)
		{
			if (package.Id.FamilyName == type switch
			    {
				    MinecraftGameTypeVersion.Release => "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
				    MinecraftGameTypeVersion.Preview => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
				    MinecraftGameTypeVersion.Beta => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe"
			    })
			{
				var deploymentResult = await packageManager.RemovePackageAsync(
					package.Id.FullName,
					RemovalOptions.PreserveApplicationData).AsTask();
				return deploymentResult;
			}
		}

		return null;
	}
	/// <summary>
	/// Retrieves the download URI for a package based on its metadata
	/// </summary>
	/// <param name="metadata">The metadata string which may be a direct URI or an identifier to resolve</param>
	/// <returns>The resolved download URI as a string</returns>
	/// <exception cref="BedrockCoreNoAvailbaleVersionUri">Thrown when no available URI is found for the metadata</exception>
	private static async Task<string> GetPackageUri([NotNull] string metadata)
	{
		if (metadata.StartsWith("http"))
			return metadata;

		try
		{
			var uri = await UpdateIDHelper.GetUriAsync(metadata);
			if (string.IsNullOrEmpty(uri))
				throw new BedrockCoreNoAvailbaleVersionUri("There is no available uri for this");
			return uri;
		}
		catch
		{
			throw;
		}
	}
}