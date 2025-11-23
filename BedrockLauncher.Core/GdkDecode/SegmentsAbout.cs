using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockLauncher.Core.GdkDecode
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1, Size = 0x10)]
	public  struct SegmentsAbout
	{
		/* 0x0 */
		public SegmentMetadataFlags Flags;
		/* 0x2 */
		public ushort PathLength;
		/* 0x4 */
		public uint PathOffset;
		/* 0x8 */
		public ulong FileSize;
	}
	[Flags]
	public enum SegmentMetadataFlags : ushort
	{
		KeepEncryptedOnDisk = 1,
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
	public struct SegmentMetadataHeader
	{
	
		public uint Magic; 

		public uint Version0; 

		public uint Version1;

		public uint HeaderLength;

		public uint SegmentCount;

		public uint FilePathsLength; 

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]

		public byte[] PDUID;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3c)]

		public byte[] Unknown;
	}
}
