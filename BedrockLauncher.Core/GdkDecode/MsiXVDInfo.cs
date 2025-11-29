using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockLauncher.Core.GdkDecode
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct XvcEncryptionKeyId
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
		public byte[] KeyId;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
	public struct XvcInfo
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
		public byte[] ContentID;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xC0)]
		public XvcEncryptionKeyId[] EncryptionKeyIds;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
		public byte[] Description;

		public UInt32 Version;
		public UInt32 RegionCount;
		public UInt32 Flags;
		public UInt16 PaddingD1C;
		public UInt16 KeyCount;
		public UInt32 UnknownD20;
		public UInt32 InitialPlayRegionId;
		public UInt64 InitialPlayOffset;
		public Int64 FileTimeCreated;
		public UInt32 PreviewRegionId;
		public UInt32 UpdateSegmentCount;
		public UInt64 PreviewOffset;
		public UInt64 UnusedSpace;
		public UInt32 RegionSpecifierCount;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x54)]
		public byte[] ReservedD54;
	}
	public enum XvcRegionId : uint
	{
		MetadataXvc = 0x40000001,
		MetadataFilesystem = 0x40000002,
		Unknown = 0x40000003,
		EmbeddedXvd = 0x40000004,
		Header = 0x40000005,
		MutableData = 0x40000006
	}
	[Flags]
	public enum XvcRegionFlags : uint
	{
		Resident = 1,
		InitialPlay = 2,
		Preview = 4,
		FileSystemMetadata = 8,
		Present = 0x10,
		OnDemand = 0x20,
		Available = 0x40,
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public struct XvcRegionHeader
	{
		public XvcRegionId Id;
		public UInt16 KeyId;
		public UInt16 Padding6;
		public XvcRegionFlags Flags;
		public UInt32 FirstSegmentIndex;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
		public string Description;

		public UInt64 Offset;
		public UInt64 Length;
		public UInt64 Hash;

		public UInt64 Unknown68;
		public UInt64 Unknown70;
		public UInt64 Unknown78;
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct XvcUpdateSegment
	{
		public UInt32 PageNum;
		public UInt64 Hash;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public struct XvcRegionSpecifier
	{
		public XvcRegionId RegionId;
		public UInt32 Padding4;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
		public string Key;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
		public string Value;
	}
}
