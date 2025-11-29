using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Windows.Devices.Display.Core;
using Microsoft.VisualBasic;
using BedrockLauncher.Core.Utils;

namespace BedrockLauncher.Core.GdkDecode;

public class MsiXVDStream : IDisposable
{
	private const ulong XVD_HEADER_INCL_SIGNATURE_SIZE = 0x3000;
	public string[] EncryptionKeys { get; private set; }
	public MsiXVDHeader Header { get; private set; }
	public bool IsEncrypted { get; private set; }
	public readonly BinaryReader Reader;
	public FileStream XvdFileStream;
	private ulong HashTreePageCount;
	private ulong HashTreePageOffset;
	private ulong MutableDataOffset;
	private bool DataIntegrity;
	private bool Resiliency;
	private ulong HashTreeLevels;
	private ulong XvdUserDataOffset;
	private bool HasSegmentMetadata;
	private SegmentMetadataHeader SegmentMetadataHeaders;
	private SegmentsAbout[] Segments;
	private string[] _segmentPaths;
	public XvcInfo XvcInfo;
	private XvcRegionHeader[] XvcRegions;
	private XvcUpdateSegment[] XvcUpdateSegments;
	private XvcRegionSpecifier[] XvcRegionSpecifiers;
	private UserDataHeader UserDataHeader;
	private bool HasUserDataFile;
	private UserDataPackageFilesHeader UserDataPackageFiles;
	private Dictionary<string, UserDataPackageFileEntry> UserDataPackages = new Dictionary<string, UserDataPackageFileEntry>();
	private Dictionary<string, byte[]> UserDataPackageContents = new Dictionary<string, byte[]>();
	private int HashEntryLength;
	public MsiXVDStream(string fileUri)
	{
		if (!File.Exists(fileUri))
			throw new FileNotFoundException("Can't found the file");
		XvdFileStream = File.Open(fileUri, FileMode.Open, FileAccess.ReadWrite);
		Reader = new BinaryReader(XvdFileStream);
		Parse();
		
	}
	private void Parse()
	{
		XvdFileStream.Position = 0;
		ParseFileHeader();
		Resiliency = Header.Volumes.HasFlag(MsiXVDVolumeAttributes.ResiliencyEnabled);
		DataIntegrity = !Header.Volumes.HasFlag(MsiXVDVolumeAttributes.DataIntegrityDisabled);
		HashTreePageCount = CalculateNumberHashPages(out HashTreeLevels,Header.NumberOfHashedPages,Resiliency);
		MutableDataOffset = Extensions.PageToOffset(Header.EmbeddedXvdPageCount) + XVD_HEADER_INCL_SIGNATURE_SIZE;
		HashTreePageOffset = Header.MutableDataLength + MutableDataOffset;
		XvdUserDataOffset = (DataIntegrity ? Extensions.PageToOffset(HashTreePageCount) : 0) + HashTreePageOffset;
		ParaseUserData();
		if (UserDataPackageContents.ContainsKey("SegmentMetadata.bin"))
		{
			ParseSegment();
		}
		ParseArea();
		List<string> strings = new List<string>();
		for (int i = 0; i < XvcInfo.EncryptionKeyIds.Length; i++)
		{
			var key = new Guid(XvcInfo.EncryptionKeyIds[i].KeyId).ToString();
			if (key == "00000000-0000-0000-0000-000000000000")
			{
				continue;
			}
			strings.Add(key);
		}

		EncryptionKeys = strings.ToArray();
	}
	

	private void ParseFileHeader()
	{
		var sizeOf = Marshal.SizeOf(typeof(MsiXVDHeader));
		var readBytes = Reader.ReadBytes(sizeOf);
		var header = Extensions.GetstructFromBytes<MsiXVDHeader>(readBytes);
		Header = header;
		IsEncrypted = !header.Volumes.HasFlag(MsiXVDVolumeAttributes.EncryptionDisabled);
		HashEntryLength = IsEncrypted ? 0x14 : 0x18;
	    
	}
	private void ParaseUserData()
	{
		XvdFileStream.Position = (long)XvdUserDataOffset;

		var userDataBuffer = new byte[Header.UserDataLength];
		var bytesRead = XvdFileStream.Read(userDataBuffer.AsSpan());
		
		using var binaryReader = new BinaryReader(new MemoryStream(userDataBuffer));
		byte[] bytes = binaryReader.ReadBytes(Marshal.SizeOf(typeof(UserDataHeader)));
		this.UserDataHeader = Extensions.GetstructFromBytes<UserDataHeader>(bytes);
		if (UserDataHeader.Type == UserDataType.PackageFiles)
		{
			HasUserDataFile = true;

			binaryReader.BaseStream.Position = UserDataHeader.Length;
			byte[] bytes_ = binaryReader.ReadBytes(Marshal.SizeOf(typeof(UserDataPackageFilesHeader)));
			UserDataPackageFiles = Extensions.GetstructFromBytes<UserDataPackageFilesHeader>(bytes_);
			
			var fileEntriesCount = (int)UserDataPackageFiles.FileCount;

			UserDataPackages.EnsureCapacity(fileEntriesCount);

			foreach (var fileEntry in Extensions.GetstructArraysFromBytes<UserDataPackageFileEntry>(binaryReader.ReadBytes(Marshal.SizeOf(typeof(UserDataPackageFileEntry))*fileEntriesCount),fileEntriesCount))
			{
				binaryReader.BaseStream.Position = UserDataHeader.Length + fileEntry.Offset;
				var fileData = new byte[fileEntry.Size];
				bytesRead = binaryReader.BaseStream.Read(fileData.AsSpan());
				Debug.Assert(bytesRead == fileEntry.Size, "bytesRead == fileEntry.Size");

				UserDataPackages[fileEntry.FilePath] = fileEntry;
				UserDataPackageContents[fileEntry.FilePath] = fileData;
			}
		}
	}
	private void ParseArea()
	{
		var xvcInfoOffset = Extensions.PageToOffset(Header.UserDataPageCount) + XvdUserDataOffset;
		XvdFileStream.Position = (int)xvcInfoOffset;
		var xvcInfo = new byte[Header.XvcDataLength];
		var xvcInfoSpan = xvcInfo.AsSpan();
		var _ = XvdFileStream.Read(xvcInfo.AsSpan());
		using var xvcInfoReader = new BinaryReader(new MemoryStream(xvcInfo));
		XvcInfo = Extensions.GetstructFromBytes<XvcInfo>(xvcInfoReader.ReadBytes(Marshal.SizeOf(typeof(XvcInfo))));
		if (XvcInfo.Version>=1)
		{
			XvcRegions = Extensions.GetstructArraysFromBytes<XvcRegionHeader>(xvcInfoReader.ReadBytes((int)(Marshal.SizeOf(typeof(XvcRegionHeader))*(XvcInfo.RegionCount))),XvcInfo.RegionCount);
			XvcUpdateSegments = Extensions.GetstructArraysFromBytes<XvcUpdateSegment>(xvcInfoReader.ReadBytes((int)(Marshal.SizeOf(typeof(XvcUpdateSegment)) * (XvcInfo.UpdateSegmentCount))), XvcInfo.UpdateSegmentCount);

			if (XvcInfo.Version >= 2)
			{
				XvcRegionSpecifiers = Extensions.GetstructArraysFromBytes<XvcRegionSpecifier>(xvcInfoReader.ReadBytes((int)(Marshal.SizeOf(typeof(XvcRegionSpecifier)) * (XvcInfo.RegionSpecifierCount))), XvcInfo.RegionSpecifierCount);
			}
		}
	}
	private void ParseSegment()
	{
		var segmentMetadataData = UserDataPackageContents["SegmentMetadata.bin"];

		using var segmentMetadataStreamReader = new BinaryReader(new MemoryStream(segmentMetadataData));

		SegmentMetadataHeaders = Extensions.GetstructFromBytes<SegmentMetadataHeader>(segmentMetadataStreamReader.ReadBytes(Marshal.SizeOf(typeof(SegmentMetadataHeader))));
		HasSegmentMetadata = true;

		Segments = Extensions.GetstructArraysFromBytes<SegmentsAbout>(segmentMetadataStreamReader.ReadBytes(Marshal.SizeOf(typeof(SegmentsAbout))*(int)SegmentMetadataHeaders.SegmentCount), (int)SegmentMetadataHeaders.SegmentCount);
	
		_segmentPaths = new string[SegmentMetadataHeaders.SegmentCount];

		var segmentPathsStartOffset =
			SegmentMetadataHeaders.HeaderLength
			+ SegmentMetadataHeaders.SegmentCount * 0x10;

		for (int segmentIndex = 0; segmentIndex < Segments.Length; segmentIndex++)
		{
			var currentSegment = Segments[segmentIndex];

			segmentMetadataStreamReader.BaseStream.Position = segmentPathsStartOffset + currentSegment.PathOffset;

			var stringDataSpan = segmentMetadataStreamReader.ReadBytes(currentSegment.PathLength * 2).AsSpan();

			_segmentPaths[segmentIndex] = new string(MemoryMarshal.Cast<byte, char>(stringDataSpan));
		}
	}
	private static ulong CalculateNumberHashPages(out ulong hashTreeLevels, ulong hashedPagesCount, bool resilient)
	{

		const ulong PAGE_SIZE = 0x1000;
		const uint HASH_ENTRY_LENGTH = 0x18;
		const uint HASH_ENTRIES_IN_PAGE = (uint)(PAGE_SIZE / HASH_ENTRY_LENGTH); // 0xAA

		const uint DATA_BLOCKS_IN_LEVEL0_HASHTREE = HASH_ENTRIES_IN_PAGE; // 0xAA
		const uint DATA_BLOCKS_IN_LEVEL1_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL0_HASHTREE; // 0x70E4
		const uint DATA_BLOCKS_IN_LEVEL2_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL1_HASHTREE; // 0x4AF768
		const uint DATA_BLOCKS_IN_LEVEL3_HASHTREE = HASH_ENTRIES_IN_PAGE * DATA_BLOCKS_IN_LEVEL2_HASHTREE; // 0x31C84B10


		ulong hashTreePageCount = (hashedPagesCount + HASH_ENTRIES_IN_PAGE - 1) / HASH_ENTRIES_IN_PAGE;
		hashTreeLevels = 1;

		if (hashTreePageCount > 1)
		{
			ulong result = 2;
			while (result > 1)
			{
			
				ulong hashBlocks = 0;
				switch (hashTreeLevels)
				{
					case 0:
						hashBlocks = (hashedPagesCount + DATA_BLOCKS_IN_LEVEL0_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL0_HASHTREE;
						break;
					case 1:
						hashBlocks = (hashedPagesCount + DATA_BLOCKS_IN_LEVEL1_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL1_HASHTREE;
						break;
					case 2:
						hashBlocks = (hashedPagesCount + DATA_BLOCKS_IN_LEVEL2_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL2_HASHTREE;
						break;
					case 3:
						hashBlocks = (hashedPagesCount + DATA_BLOCKS_IN_LEVEL3_HASHTREE - 1) / DATA_BLOCKS_IN_LEVEL3_HASHTREE;
						break;
				}

				result = hashBlocks;
				hashTreeLevels += 1;
				hashTreePageCount += result;
			}
		}

		if (resilient)
			hashTreePageCount *= 2;

		return hashTreePageCount;
	}
	public async Task ExtractTaskAsync(string output,MsiXVDDecoder decoder,Progress<DecompressProgress>? progress,CancellationToken cts = default)
	{
		await Task.Run((() =>
		{
			var firstSegmentOffset = Extensions.PageToOffset(XvcUpdateSegments[0].PageNum);
			XvcRegionHeader[] extractableRegionList =
				XvcRegions
					.Where(region =>
						(region.FirstSegmentIndex != 0 || firstSegmentOffset == region.Offset))
					.ToArray(); ;
			for (int i = 0; i < extractableRegionList.Length; i++)
			{
				var region = extractableRegionList[i];
				if (cts.IsCancellationRequested)
				{
					return;
				}
				ExtractPart(
					progress,
					output,
					decoder,
					(uint)region.Id,
					region.Offset,
					region.Length,
					region.FirstSegmentIndex,
					IsEncrypted && region.KeyId != (0xffff),
					cts);
			}
		}));
		
	}
	private ulong CalculateHashEntryBlockOffset(ulong blockNo, out ulong hashEntryId)
	{
		var hashBlockPage = Extensions.ComputeHashBlockIndexForDataBlock(Header.Kind, HashTreeLevels,
			Header.NumberOfHashedPages, blockNo, 0, out hashEntryId, Resiliency);

		return
			HashTreePageOffset
			+ Extensions.PageToOffset(hashBlockPage);
	}
	private void ExtractPart(
	IProgress<DecompressProgress>? progressTask,
	string outputDirectory,
	 MsiXVDDecoder decryptor,
	uint headerId,
	ulong regionStartOffset,
	ulong regionLength,
	uint startSegmentIndex,
	bool shouldDecrypt,
	CancellationToken cts
)	
	{
		var tweakInitializationVector = (stackalloc byte[16]);

		if (shouldDecrypt)
		{
			MemoryMarshal.Cast<byte, uint>(tweakInitializationVector)[1] = headerId;
			Header.VdUid.AsSpan(0, 8).CopyTo(tweakInitializationVector[8..]);
		}

		var shouldRefreshPageCache = true;
		var totalPageCacheOffset = (long)regionStartOffset;
		var pageCacheOffset = 0;
		var pageCache = new byte[0x100000].AsSpan();
		var shouldRefreshHashCache = DataIntegrity;
		var totalHashCacheOffset =
			(long)CalculateHashEntryBlockOffset(Extensions.GetPageOffset(regionStartOffset - XvdUserDataOffset),
				out var hashCacheEntryIndex);

		var hashCacheOffset = (int)(hashCacheEntryIndex * 0x18);
		var hashCache = new byte[0x100000].AsSpan();
		var currentSegmentIndex = startSegmentIndex;
		var processedPageCount = 0;
		var totalPageCount = (long)Extensions.GetPageOffset(regionLength);

		while (Segments.Length > currentSegmentIndex && totalPageCount > processedPageCount)
		{
			if (cts.IsCancellationRequested)
			{
				return;
			}
			var segmentFileSize = Segments[currentSegmentIndex].FileSize;
			var segmentFilePath = _segmentPaths[currentSegmentIndex];

			var outputFilePath = Path.Join(outputDirectory, segmentFilePath);
			var outputFileDirectory = Path.GetDirectoryName(outputFilePath);
			if (outputFileDirectory != null)
				Directory.CreateDirectory(outputFileDirectory);

			using var outputFileStream = File.OpenWrite(outputFilePath);

			var remainingSegmentSize = segmentFileSize;

			do 
			{
				var currentChunkSize = (int)Math.Min(remainingSegmentSize, 0x1000);

				int bytesRead;
				if (shouldRefreshHashCache)
				{
					XvdFileStream.Position = totalHashCacheOffset;
					bytesRead = XvdFileStream.Read(hashCache);
					Debug.Assert(bytesRead == hashCache.Length);
					shouldRefreshHashCache = false;
				}

				if (shouldRefreshPageCache)
				{
					XvdFileStream.Position = totalPageCacheOffset;
					bytesRead = XvdFileStream.Read(pageCache);
					Debug.Assert(bytesRead == pageCache.Length || (uint)pageCache.Length > remainingSegmentSize);
					shouldRefreshPageCache = false;
				}

				var currentPageData = pageCache.Slice(pageCacheOffset, 0x1000);

				if (DataIntegrity)
				{
					var currentHashEntry = hashCache.Slice(hashCacheOffset, 0x18);

					

					if (shouldDecrypt)
					{
						MemoryMarshal.Cast<byte, uint>(tweakInitializationVector)[0] =
							MemoryMarshal.Cast<byte, uint>(currentHashEntry.Slice(HashEntryLength, sizeof(uint)))[0];
					}

					hashCacheOffset += 0x18;
					hashCacheEntryIndex++;
					if (hashCacheEntryIndex == 0xaa)
					{
						hashCacheEntryIndex = 0;
						hashCacheOffset += 0x10; 
					}

					if (hashCacheOffset == hashCache.Length)
					{
						totalHashCacheOffset += hashCacheOffset;
						hashCacheOffset = 0;
						hashCacheEntryIndex = 0;
						shouldRefreshHashCache = true;
					}
				}

				if (shouldDecrypt)
				{
					decryptor.Decrypt(currentPageData, currentPageData, tweakInitializationVector);
				}

				outputFileStream.Write(currentPageData[..currentChunkSize]);

				remainingSegmentSize -= (uint)currentChunkSize;

				pageCacheOffset += 0x1000;
				if (pageCacheOffset == pageCache.Length)
				{
					totalPageCacheOffset += pageCacheOffset;
					pageCacheOffset = 0;
					shouldRefreshPageCache = true;
				}

				processedPageCount++;
				progressTask?.Report((new DecompressProgress()
				{
					CurrentCount = currentSegmentIndex,
					FileName = segmentFilePath,
					TotalCount = Segments.Length
				}));
			} while (remainingSegmentSize > 0);
			currentSegmentIndex++;
		}
	}

	public void Dispose()
	{
		XvdFileStream.Dispose();
		GC.Collect();
	}
}