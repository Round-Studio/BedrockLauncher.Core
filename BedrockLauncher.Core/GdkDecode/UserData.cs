using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockLauncher.Core.GdkDecode
{
	public enum UserDataType : UInt32
	{
		PackageFiles = 0
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public struct UserDataHeader
	{
		public UInt32 Length;
		public UInt32 Version;
		public UserDataType Type;
		public UInt32 Unknown;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public struct UserDataPackageFilesHeader
	{
		public UInt32 Version;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string PackageFullName;
		public UInt32 FileCount;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public struct UserDataPackageFileEntry
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string FilePath;
		public UInt32 Size;
		public UInt32 Offset;
	}
}
