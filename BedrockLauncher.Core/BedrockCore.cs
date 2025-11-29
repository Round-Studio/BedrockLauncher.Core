using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BedrockLauncher.Core.CoreOption;
using BedrockLauncher.Core.DependsComplete;
using BedrockLauncher.Core.Utils;
using BedrockLauncher.Core.UwpRegister;
using Microsoft.Win32;
using Windows.Management.Deployment;
using BedrockLauncher.Core.GdkDecode;

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
	public CoreOptions? Options { get; set; }

	/// <summary>
	///     Init BedrockCore
	/// </summary>
	public async Task InitAsync()
	{
	  await	Task.Run((() =>
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
	/// <summary>
	/// Check devices' vc runtime
	/// </summary>
	/// <returns>The first value is for uwp,second is win32.True if the required VC runtime is installed, otherwise false</returns>
	public (bool,bool) IsHasVCRuntime(Architecture arch)
	{
		try
		{
			bool CheckVersion(string[] archli)
			{
				foreach (var s in archli)
				{
					using (RegistryKey key = Registry.LocalMachine.OpenSubKey(s))
					{
						if (key != null)
						{
							return true;
						}
					}
				}

				return false;
			}
			bool isHasVCwin32 = false;
			bool isHasVCUwp = false;
			string[] registryPaths = arch switch
			{
				Architecture.X64 => new []{ @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64",@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" },
				Architecture.X86 => new []{ @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x86" , @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86" },
				Architecture.Arm64 => new []{ @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\arm64" , @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\arm64" },
				_=> new[] { @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64", @"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64" }
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
			return (false,false);
		}
	}
	public async Task InstallPackageAsync(LocalGamePackageOptions options)
	{
		Directory.CreateDirectory(options.InstallDstFolder);
		if (options.Type == MinecraftBuildTypeVersion.GDK)
		{
			await Task.Run((async () =>
			{
				CikKey cik = new CikKey(options.GameTypeVersion switch
				{
					MinecraftGameTypeVersion.Release => _DEFINE_REF2.rel,
					MinecraftGameTypeVersion.Preview => _DEFINE_REF2.pre,
					MinecraftGameTypeVersion.Beta => _DEFINE_REF2.pre
				});
				var msiXvdDecoder = new MsiXVDDecoder(cik);
				var msiXvdStream = new MsiXVDStream(options.FileFullPath);
				await msiXvdStream.ExtractTaskAsync(Path.GetFullPath(options.InstallDstFolder), msiXvdDecoder,
					options.ExtractionProgress, options.CancellationToken.GetValueOrDefault());
			}));
			return;
		}

		if (options.Type == MinecraftBuildTypeVersion.UWP)
		{
			 options.InstallStates?.Report(InstallStates.Extracting);

			 await ZipExtractor.ExtractWithProgressAsync(options.FileFullPath, options.InstallDstFolder,
				progress: options.ExtractionProgress, options.CancellationToken.GetValueOrDefault());
			
			 options.InstallStates?.Report(InstallStates.Extracted);

			 await ManifestEditor.EditManifest(options.InstallDstFolder,TimeBasedVersion.GetVersion(),options.BackGroundConfig);

			 var config = new DeploymentOptionsConfig();
			 config.PackagePath = Path.Combine(options.InstallDstFolder, "AppxManifest.xml");
			 config.CancellationToken = options.CancellationToken.GetValueOrDefault();
			 config.Timeout = new TimeSpan(0,0,3);
			 config.DeploymentOptions = DeploymentOptions.DevelopmentMode | DeploymentOptions.ForceUpdateFromAnyVersion;
			 config.ProgressCallback = options.DeployProgress;

			 options.InstallStates?.Report(InstallStates.Registering);
			 var result = await UwpRegister.UwpRegister.RegisterAppxAsync(config);
			 options.InstallStates?.Report(InstallStates.Registered);
			 options.DeploymentResult = result;
		}

	}
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
			gamePackage.DownloadProgress,
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
	private static async Task<string>  GetPackageUri([NotNull]string metadata)
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
