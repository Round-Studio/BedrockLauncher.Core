#pragma warning disable CS8524
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.DependsComplete;
using BedrockLauncher.Core.GdkDecode;
using BedrockLauncher.Core.Utils;
using BedrockLauncher.Core.UwpRegister;
using Microsoft.Win32;
using Windows.Foundation.Collections;
using Windows.Management.Deployment;
using Windows.System;

namespace BedrockLauncher.Core;

public class BedrockCore
{

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
		
			if (Options.IsAutoOpenDevelopment)
			{
				if (!GetWindowsDevelopmentState())
				{
					throw new BedrockCoreException("Windows Developer Mode is required for non-admin UWP loose package registration. Please enable Developer Mode in Windows settings.");
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

			if (Options.IsAutoCompleteGameInput)
			{
			 	await AutoCompleteGameInput();
			}

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
	///     Opens Windows Settings so the current user can enable Developer Mode without requiring this process to run elevated.
	/// </summary>
	/// <returns>true if the settings page was opened; otherwise, false.</returns>
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
			var packages = packageManager.FindPackagesForUser(string.Empty);

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
			config.DeploymentOptions = is_installed
				? DeploymentOptions.DevelopmentMode | DeploymentOptions.ForceUpdateFromAnyVersion
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
	private static async Task<Process> WaitForProcessAsync(string processName, DateTime startTime, TimeSpan timeout)
	{
		var stopwatch = Stopwatch.StartNew();

		while (stopwatch.Elapsed < timeout)
		{
			var processes = Process.GetProcessesByName(processName);

			if (processes.Length > 0)
			{
				return processes
					.Where(p => p.StartTime > startTime)
					.OrderBy(p => (p.StartTime - startTime).TotalMilliseconds)
					.FirstOrDefault();
			}

		
			await Task.Delay(200);
		}

		return null;
	}
	private static DateTime GetStartTimeSafe(Process proc)
	{
		try
		{
			return proc.StartTime;
		}
		catch
		{
			return DateTime.MinValue;
		}
	}
	/// <summary>
	/// Launch the Minecraft game process based on the specified launch options
	/// </summary>
	/// <param name="options">The launch options containing game folder, arguments, and build type</param>
	/// <returns>The Process object representing the launched game instance</returns>
	public async Task<Process> LaunchGameAsync(LaunchOptions options)
	{
		var process = new Process();
		if (options.MinecraftBuildType == MinecraftBuildTypeVersion.GDK)
		{
			options.Progress?.Report(LaunchState.Launching);
			string targetExe = "Minecraft.Windows.exe";
			string fullPath = Path.Combine(options.GameFolder, targetExe);
			string processName = Path.GetFileNameWithoutExtension(targetExe); 

		
			var beforeSnapshot = Process.GetProcessesByName(processName)
				.ToDictionary(p => p.Id, p => GetStartTimeSafe(p));

	
			DateTime launchTime = DateTime.Now;
			Process.Start(new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = fullPath,
				UseShellExecute = true
			});

			Process minecraftProcess = null;

	
			for (int i = 0; i < 10; i++)
			{
				var currentProcesses = Process.GetProcessesByName(processName);
				foreach (var proc in currentProcesses)
				{

					if (beforeSnapshot.ContainsKey(proc.Id))
						continue;


					DateTime startTime = GetStartTimeSafe(proc);
					if (startTime == DateTime.MinValue)
						continue; 

					if (startTime >= launchTime.AddMilliseconds(-200))
					{
						minecraftProcess = proc;
						break;
					}
				}

				if (minecraftProcess != null)
					break;

				Thread.Sleep(500);
				
			}
			process = minecraftProcess;

			options.Progress?.Report(LaunchState.Launched);
		}

		if (options.MinecraftBuildType == MinecraftBuildTypeVersion.UWP)
		{
			string manifest = Path.Combine(options.GameFolder, "AppxManifest.xml");
			string packageFamily = options.GameType switch
			{
				MinecraftGameTypeVersion.Release => "Microsoft.MinecraftUWP_8wekyb3d8bbwe",
				MinecraftGameTypeVersion.Preview => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe",
				MinecraftGameTypeVersion.Beta => "Microsoft.MinecraftWindowsBeta_8wekyb3d8bbwe"
			};
			string packageName = options.GameType switch
			{
				MinecraftGameTypeVersion.Beta => "Microsoft.MinecraftWindowsBeta",
				MinecraftGameTypeVersion.Release => "Microsoft.MinecraftUWP",
				MinecraftGameTypeVersion.Preview => "Microsoft.MinecraftWindowsBeta"
			};
			if (!File.Exists(manifest))
			{
				throw new IOException("File doesn't exist");
			}
			PackageManager packageManager = new PackageManager();
			bool twice_launch = false;
			foreach (var package in packageManager.FindPackagesForUser(string.Empty))
			{
				if (package.Id.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase))
				{
					if (Path.GetFullPath(package.InstalledPath) == Path.GetFullPath(options.GameFolder))
					{
						twice_launch = true;
					}
				}
			}
			bool is_installed = UwpRegister.UwpRegister.IsPackageInstalled(packageName);
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

			if (options.Old_VersionLaunching)
			{
				var appDiagnosticInfos = AppDiagnosticInfo.RequestInfoForPackageAsync(packageFamily).AsTask().Result;
				if (appDiagnosticInfos.Count != 0)
				{
					 await appDiagnosticInfos[0].LaunchAsync();
				}
			}
			else
			{
				var options_st = new LauncherOptions
				{
					TargetApplicationPackageFamilyName = packageFamily
				};
				if (options?.LaunchArgs == string.Empty)
				{
					await Launcher.LaunchUriAsync(new Uri(options.LaunchArgs), options_st);
				}
				else
				{
					await Launcher.LaunchUriAsync(new Uri("minecraft://launch"), options_st);
				}
				
			}
			Process[] processes = Process.GetProcessesByName("Minecraft.Windows");
			Process[] process_oldVersion = Process.GetProcessesByName("Minecraft.Win10.DX11");
			processes = processes.Concat(process_oldVersion).ToArray();
			process = processes.OrderBy(p => p.StartTime).Last();
		}
		return process;
	}
	/// <summary>
	/// Remove Uwp Minecraft Game
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	public  async Task<DeploymentResult?> RemoveUWPGameAsync(MinecraftGameTypeVersion type)
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
	
	private  async Task<string> GetPackageUriInside([NotNull] string metadata)
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
	/// <summary>
	/// Retrieves the download URI for a package based on its metadata
	/// </summary>
	/// <returns>The resolved download URI as a string</returns>
	/// <exception cref="BedrockCoreNoAvailbaleVersionUri">Thrown when no available URI is found for the metadata</exception>
	public  async Task<string> GetPackageUri(BuildInfo buildInfo,Architecture devicesArch)
	{
		var find = buildInfo.Variations.Find((variation => variation.Arch == devicesArch));
		if (find == null)
			throw new BedrockCoreException($"Unable to find {devicesArch} Version");
		if (find.MetaData.Count == 0)
			throw new BedrockCoreNoAvailbaleVersionUri("There is no available Uri to download");
		return await GetPackageUriInside(find.MetaData.Last());
	}
	/// <summary>
	/// Ensures that the GameInput runtime is installed on the system, installing it if necessary.
	/// </summary>
	/// <remarks>This method checks for the presence of the GameInput runtime using its MSI product identifier. If
	/// the runtime is not installed, it initiates the installation process. Callers can await the returned task to ensure
	/// the operation completes before proceeding.</remarks>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task AutoCompleteGameInput()
	{
		var isMsiInstalled = MsiHelper.IsMsiProductInstalledByGuid("64d0ccb1-329e-d507-0886-47e53d59ae21");
		if (!isMsiInstalled)
		{
			await VCRuntimeHelper.InstallGameInput();
		}
		return;
	}
	
}