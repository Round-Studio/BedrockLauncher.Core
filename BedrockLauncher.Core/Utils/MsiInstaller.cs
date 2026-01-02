using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace BedrockLauncher.Core.Utils
{
	public static class MsiHelper
	{
		/// <summary>
		/// Silently installs an MSI package
		/// </summary>
		/// <param name="msiFilePath">Full path to the MSI file</param>
		/// <param name="additionalArgs">Additional installation parameters</param>
		/// <returns>True if installation succeeded, false otherwise</returns>
		public static int InstallMsiSilently(string msiFilePath, string additionalArgs = "")
		{
			try
			{
				// Validate that the file exists
				if (!File.Exists(msiFilePath))
				{
					throw new IOException($"Error: MSI file not found '{msiFilePath}'");
				}

				// Use msiexec.exe to install
				Process process = new Process();
				process.StartInfo.FileName = "msiexec.exe";

				// Key parameters:
				// /i - Perform installation
				// /qn - Completely silent, no UI
				// /norestart - Do not restart after installation
				// /l*v - Log verbose output (optional, for debugging)
				string arguments = $"/i \"{msiFilePath}\" /qn /norestart {additionalArgs}";

				process.StartInfo.Arguments = arguments;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;      // Don't create a window
				process.StartInfo.RedirectStandardOutput = false; // No output needed

				process.Start();
				process.WaitForExit(); // Wait for installation to complete

				// Check exit code
				return process.ExitCode;

			}
			catch (Exception ex)
			{
				throw;
			}
		}
		public static bool IsMsiProductInstalledByGuid(string productGuid)
		{
			
			string[] registryPaths = {
				@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
				@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
			};

			using (RegistryKey localMachine = Registry.LocalMachine)
			{
				foreach (string registryPath in registryPaths)
				{
					using (RegistryKey key = localMachine.OpenSubKey(registryPath))
					{
						if (key == null) continue;

						foreach (string subKeyName in key.GetSubKeyNames())
						{
							using (RegistryKey subKey = key.OpenSubKey(subKeyName))
							{
								// 检查ProductCode
								string productCode = subKey?.GetValue("ProductCode") as string;
								if (!string.IsNullOrEmpty(productCode) &&
								    productCode.Equals(productGuid, StringComparison.OrdinalIgnoreCase))
								{
									return true;
								}

								// 或者检查UninstallString中是否包含ProductCode
								string uninstallString = subKey?.GetValue("UninstallString") as string;
								if (!string.IsNullOrEmpty(uninstallString) &&
								    uninstallString.Contains(productGuid, StringComparison.OrdinalIgnoreCase))
								{
									return true;
								}
							}
						}
					}
				}
			}

			return false;
		}
	}

}
