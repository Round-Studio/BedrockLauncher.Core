using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace BedrockLauncher.Core.GdkDecode;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct MsiXVDHeader
{
	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x200)]
	public byte[] Signature;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
	public char[] Magic;

	public MsiXVDVolumeAttributes Volumes;
	public UInt32 FormatVersion;
	public Int64 FileTimeCreated;
	public UInt64 DriveSize;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public byte[] VdUid;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public byte[] UdUid;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
	public byte[] TopHashBlockHash;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
	public byte[] OriginalXvcDataHash;

	public MsiXVDKind Kind;
	public MsiXVDContentCategory Category;
	public UInt32 EmbeddedXvdLength;
	public UInt32 UserDataLength;
	public UInt32 XvcDataLength;
	public UInt32 DynamicHeaderLength;
	public UInt32 BlockSize;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
	public ExtEntry[] ExtEntries;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
	public UInt16[] Capabilities;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
	public byte[] PeCatalogHash;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public byte[] EmbeddedXvdPdUid;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public byte[] Reserved13C;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
	public byte[] KeyMaterial;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
	public byte[] UserDataHash;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public char[] SandboxId;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public byte[] ProductId;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public byte[] PdUid;

	public UInt16 PackageVersion1;
	public UInt16 PackageVersion2;
	public UInt16 PackageVersion3;
	public UInt16 PackageVersion4;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public UInt16[] PeCatalogCaps;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x80)]
	public byte[] PeCatalogs;

	public UInt32 WriteableExpirationDate;
	public UInt32 WriteablePolicyFlags;
	public UInt32 PersistentLocalStorageSize;

	public Byte MutableDataPageCount;
	public Byte Unknown271;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
	public byte[] Unknown272;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xA)]
	public byte[] Reserved282;

	public Int64 SequenceNumber;
	public UInt16 Unknown1;
	public UInt16 Unknown2;
	public UInt16 Unknown3;
	public UInt16 Unknown4;

	public MsiXVDOdkIndex OdkKeyslotId;

	[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xB54)]
	public byte[] Reserved2A0;

	public UInt64 ResilientDataOffset;
	public UInt32 ResilientDataLength;
	public ulong MutableDataLength => Extensions.PageToOffset(MutableDataPageCount);
	public ulong UserDataPageCount => Extensions.BytesToPages(UserDataLength);
	public ulong XvcInfoPageCount => Extensions.BytesToPages(XvcDataLength);
	public ulong EmbeddedXvdPageCount => Extensions.BytesToPages(EmbeddedXvdLength);
	public ulong DynamicHeaderPageCount => Extensions.BytesToPages(DynamicHeaderLength);
	public ulong DrivePageCount => Extensions.BytesToPages(DriveSize);
	public ulong NumberOfHashedPages => DrivePageCount + UserDataPageCount + XvcInfoPageCount + DynamicHeaderPageCount;
	public ulong NumberOfMetadataPages => UserDataPageCount + XvcInfoPageCount + DynamicHeaderPageCount;
}

#region Generated from cxx

public struct ExtEntry
{
	public uint Code;
	public uint Length;
	public ulong Offset;
	public uint DataLength;
	public uint Reserved;
}

public enum MsiXVDKind : uint
{
	Fixed = 0,
	Dynamic = 1
}

public enum MsiXVDOdkIndex : uint
{
	StandardOdk = 0,
	GreenOdk = 1,
	RedOdk = 2,
	Invalid = 0xFFFFFFFF
}

public enum MsiXVDContentCategory : uint
{
	Data = 0,
	Title = 1,
	SystemOS = 2,
	EraOS = 3,
	Scratch = 4,
	ResetData = 5,
	Application = 6,
	HostOS = 7,
	X360STFS = 8,
	X360FATX = 9,
	X360GDFX = 0xA,
	Updater = 0xB,
	OfflineUpdater = 0xC,
	Template = 0xD,
	MteHost = 0xE,
	MteApp = 0xF,
	MteTitle = 0x10,
	MteEraOS = 0x11,
	EraTools = 0x12,
	SystemTools = 0x13,
	SystemAux = 0x14,
	AcousticModel = 0x15,
	SystemCodecsVolume = 0x16,
	QasltPackage = 0x17,
	AppDlc = 0x18,
	TitleDlc = 0x19,
	UniversalDlc = 0x1A,
	SystemDataVolume = 0x1B,
	TestVolume = 0x1C,
	HardwareTestVolume = 0x1D,
	KioskContent = 0x1E,
	HostProfiler = 0x20,
	Uwa = 0x21,
	Unknown22 = 0x22,
	Unknown23 = 0x23,
	Unknown24 = 0x24,
	ServerAgent = 0x25
}

[Flags]
public enum MsiXvcAreaAttributes : uint
{
	Resident = 1,
	InitialPlay = 2,
	Preview = 4,
	FileSystemMetadata = 8,
	Present = 0x10,
	OnDemand = 0x20,
	Available = 0x40
}

[Flags]
public enum MsiXVDVolumeAttributes : uint
{
	ReadOnly = 1,
	EncryptionDisabled = 2,
	DataIntegrityDisabled = 4,
	LegacySectorSize = 8,
	ResiliencyEnabled = 0x10,
	SraReadOnly = 0x20,
	RegionIdInXts = 0x40,
	EraSpecific = 0x80
}

[Flags]
public enum MsiXvcAreaPresenceInfo : byte
{
	IsPresent = 1,
	IsAvailable = 2,

	Disc1 = 0x10,
	Disc2 = 0x20,
	Disc3 = 0x30,
	Disc4 = 0x40,
	Disc5 = 0x50,
	Disc6 = 0x60,
	Disc7 = 0x70,
	Disc8 = 0x80,
	Disc9 = 0x90,
	Disc10 = 0xA0,
	Disc11 = 0xB0,
	Disc12 = 0xC0,
	Disc13 = 0xD0,
	Disc14 = 0xE0,
	Disc15 = 0xF0
}

public enum MsiXVDUserDataCategory : uint
{
	PackageFiles = 0
}

[Flags]
public enum MsiXVDSegmentMetadataFlags : ushort
{
	KeepEncryptedOnDisk = 1
}

#endregion