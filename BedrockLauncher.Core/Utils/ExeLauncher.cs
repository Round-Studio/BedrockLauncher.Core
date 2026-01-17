using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace BedrockLauncher.Core.Utils
{
	public class ExeLauncher
	{
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern bool CreateProcessWithTokenW(
	  IntPtr hToken,
	  LogonFlags dwLogonFlags,
	  string lpApplicationName,
	  string lpCommandLine,
	  CreationFlags dwCreationFlags,
	  IntPtr lpEnvironment,
	  string lpCurrentDirectory,
	  ref STARTUPINFO lpStartupInfo,
	  out PROCESS_INFORMATION lpProcessInformation);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool DuplicateTokenEx(
			IntPtr hExistingToken,
			uint dwDesiredAccess,
			IntPtr lpTokenAttributes,
			SecurityImpersonationLevel ImpersonationLevel,
			TokenType TokenType,
			out IntPtr phNewToken);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[StructLayout(LayoutKind.Sequential)]
		private struct PROCESS_INFORMATION
		{
			public IntPtr hProcess;
			public IntPtr hThread;
			public int dwProcessId;
			public int dwThreadId;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct STARTUPINFO
		{
			public int cb;
			public string lpReserved;
			public string lpDesktop;
			public string lpTitle;
			public int dwX;
			public int dwY;
			public int dwXSize;
			public int dwYSize;
			public int dwXCountChars;
			public int dwYCountChars;
			public int dwFillAttribute;
			public int dwFlags;
			public short wShowWindow;
			public short cbReserved2;
			public IntPtr lpReserved2;
			public IntPtr hStdInput;
			public IntPtr hStdOutput;
			public IntPtr hStdError;
		}

		private enum LogonFlags
		{
			LOGON_WITH_PROFILE = 0x00000001,
			LOGON_NETCREDENTIALS_ONLY = 0x00000002
		}

		private enum CreationFlags
		{
			NORMAL_PRIORITY_CLASS = 0x00000020,
			CREATE_NO_WINDOW = 0x08000000,
			CREATE_UNICODE_ENVIRONMENT = 0x00000400
		}

		private enum TokenType
		{
			TokenPrimary = 1,
			TokenImpersonation = 2
		}

		private enum SecurityImpersonationLevel
		{
			SecurityAnonymous = 0,
			SecurityIdentification = 1,
			SecurityImpersonation = 2,
			SecurityDelegation = 3
		}

		
		public static Process LaunchWithLowPrivilege(string exePath, string arguments = "")
		{
			IntPtr userToken = IntPtr.Zero;
			IntPtr duplicateToken = IntPtr.Zero;
			PROCESS_INFORMATION procInfo = default;

			try
			{
				if (!File.Exists(exePath))
					throw new FileNotFoundException($" {exePath}");

				
				using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
				{
					userToken = identity.Token;

				
					if (!DuplicateTokenEx(
						userToken,
						0x02000000, // MAXIMUM_ALLOWED
						IntPtr.Zero,
						SecurityImpersonationLevel.SecurityIdentification,
						TokenType.TokenPrimary,
						out duplicateToken))
					{
						throw new Exception($"can't copy token: {Marshal.GetLastWin32Error()}");
					}
				}

			
				STARTUPINFO startupInfo = new STARTUPINFO();
				startupInfo.cb = Marshal.SizeOf(startupInfo);
				startupInfo.lpDesktop = "Winsta0\\Default";
				startupInfo.dwFlags = 0x00000001; // STARTF_USESHOWWINDOW
				startupInfo.wShowWindow = 1; // SW_SHOWNORMAL

				string commandLine = $"\"{exePath}\" {arguments}";
				string workingDir = Path.GetDirectoryName(exePath);

				
				bool success = CreateProcessWithTokenW(
					duplicateToken,
					LogonFlags.LOGON_WITH_PROFILE,
					null, 
					commandLine,
					CreationFlags.NORMAL_PRIORITY_CLASS | CreationFlags.CREATE_UNICODE_ENVIRONMENT,
					IntPtr.Zero, 
					workingDir,
					ref startupInfo,
					out procInfo);

				if (!success)
				{
					int error = Marshal.GetLastWin32Error();
					throw new Exception($"failed to start {error}");
				}

				
				Process process = Process.GetProcessById(procInfo.dwProcessId);

			
				if (procInfo.hProcess != IntPtr.Zero)
					CloseHandle(procInfo.hProcess);
				if (procInfo.hThread != IntPtr.Zero)
					CloseHandle(procInfo.hThread);

				return process;
			}
			catch (Exception ex)
			{
				return null;
			}
			finally
			{
		
				if (duplicateToken != IntPtr.Zero)
					CloseHandle(duplicateToken);
			}
		}
	}
}
