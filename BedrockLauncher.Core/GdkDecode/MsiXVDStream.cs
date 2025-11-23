using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.VisualBasic;

namespace BedrockLauncher.Core.GdkDecode;

public class MsiXVDStream
{
	private const ulong XVD_HEADER_INCL_SIGNATURE_SIZE = 0x3000;
	public readonly string[] EncryptionKeys;
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
	public MsiXVDStream(string fileUri, in CikKey cky)
	{
		if (!File.Exists(fileUri))
			throw new FileNotFoundException("Can't found the file");
		XvdFileStream = File.Open(fileUri, FileMode.Open, FileAccess.ReadWrite);
		Reader = new BinaryReader(XvdFileStream);
	}

	public void ParseFile()
	{
		XvdFileStream.Position = 0;
		ParseFileHeader();
		Resiliency = (Header.Volumes & MsiXVDVolumeAttributes.ResiliencyEnabled) != 0;
		DataIntegrity = (Header.Volumes & MsiXVDVolumeAttributes.DataIntegrityDisabled) != 0;
		HashTreePageCount = CalculateNumberHashPages(out HashTreeLevels,Header.NumberOfHashedPages,Resiliency);
		MutableDataOffset = Extensions.PageToOffset(Header.EmbeddedXvdPageCount) + XVD_HEADER_INCL_SIGNATURE_SIZE;
		HashTreePageOffset = Header.MutableDataLength + MutableDataOffset;
		XvdUserDataOffset = (DataIntegrity ? Extensions.PageToOffset(HashTreePageCount) : 0) + HashTreePageOffset;

	}
	

	private void ParseFileHeader()
	{
		var sizeOf = Marshal.SizeOf(typeof(MsiXVDHeader));
		var readBytes = Reader.ReadBytes(sizeOf);
		var header = Extensions.GetstructFromBytes<MsiXVDHeader>(readBytes);
		Header = header;
		IsEncrypted = !header.Volumes.HasFlag(MsiXVDVolumeAttributes.EncryptionDisabled);
	}
	private void ParseSegment()
	{
		
	}
	public static ulong CalculateNumberHashPages(out ulong hashTreeLevels, ulong hashedPagesCount, bool resilient)
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
	private ulong CalculateHashEntryBlockOffset(ulong blockNo, out ulong hashEntryId)
	{
		var hashBlockPage = Extensions.ComputeHashBlockIndexForDataBlock(Header.Kind, HashTreeLevels,
			Header.NumberOfHashedPages, blockNo, 0, out hashEntryId, Resiliency);

		return
			HashTreePageOffset
			+ Extensions.PageToOffset(hashBlockPage);
	}
	public void ExtractPart()
	{
		
	}
	private void ExtractRegion(
	IProgress<string> progressTask,
	string outputDirectory,
	MsiXVDDecoder decryptor,
	uint headerId,
	ulong regionStartOffset,
	ulong regionLength,
	uint startSegmentIndex,
	bool shouldDecrypt,
	bool skipHashVerification = false
)
	{
		var tweakInitializationVector = (stackalloc byte[16]);

		if (shouldDecrypt)
		{
			MemoryMarshal.Cast<byte, uint>(tweakInitializationVector)[1] = headerId;
			Header.VdUid.AsSpan(0, 8).CopyTo(tweakInitializationVector[8..]);
		}

		// Page Cache
		var shouldRefreshPageCache = true;
		var totalPageCacheOffset = (long)regionStartOffset;
		var pageCacheOffset = 0;
		var pageCache = new byte[0x100000].AsSpan();

		// Hash Cache
		var shouldRefreshHashCache = DataIntegrity;
		var totalHashCacheOffset =
			(long)CalculateHashEntryBlockOffset(Extensions.GetPageOffset(regionStartOffset - XvdUserDataOffset),
				out var hashCacheEntryIndex);

		var hashCacheOffset = (int)(hashCacheEntryIndex * 0x18);
		var hashCache = new byte[0x100000].AsSpan();

		// Buffer for calculated hash
		var computedHash = (stackalloc byte[SHA256.HashSizeInBytes]);

		// Progress tracking
		var currentSegmentIndex = startSegmentIndex;
		var processedPageCount = 0;
		var totalPageCount = (long)Extensions.GetPageOffset(regionLength);

		while (_segments.Length > currentSegmentIndex && totalPageCount > processedPageCount)
		{
			var segmentFileSize = _segments[currentSegmentIndex].FileSize;
			var segmentFilePath = _segmentPaths[currentSegmentIndex];

			var outputFilePath = Path.Join(outputDirectory, segmentFilePath);
			var outputFileDirectory = Path.GetDirectoryName(outputFilePath);
			if (outputFileDirectory != null)
				Directory.CreateDirectory(outputFileDirectory);

			using var outputFileStream = File.OpenWrite(outputFilePath);

			var remainingSegmentSize = segmentFileSize;

			do // Even empty files take up one page of padding data, so this loop runs at least once
			{
				var currentChunkSize = (int)Math.Min(remainingSegmentSize, XvdFile.PAGE_SIZE);

				int bytesRead;
				if (shouldRefreshHashCache)
				{
					_stream.Position = totalHashCacheOffset;
					bytesRead = _stream.Read(hashCache);
					Debug.Assert(bytesRead == hashCache.Length);
					shouldRefreshHashCache = false;
				}

				if (shouldRefreshPageCache)
				{
					_stream.Position = totalPageCacheOffset;
					bytesRead = _stream.Read(pageCache);
					Debug.Assert(bytesRead == pageCache.Length || (uint)pageCache.Length > remainingSegmentSize);
					shouldRefreshPageCache = false;
				}

				var currentPageData = pageCache.Slice(pageCacheOffset, (int)XvdFile.PAGE_SIZE);

				if (_dataIntegrity)
				{
					var currentHashEntry = hashCache.Slice(hashCacheOffset, (int)XvdFile.HASH_ENTRY_LENGTH);

					if (!skipHashVerification)
					{
						SHA256.HashData(currentPageData, computedHash);

						if (!currentHashEntry[.._hashEntryLength].SequenceEqual(computedHash[.._hashEntryLength]))
						{
							ConsoleLogger.WriteErrLine($"Page 0x{processedPageCount:x} has an invalid hash, retrying.");

							// This could be corruption during download, refresh caches and retry
							shouldRefreshHashCache = true;
							shouldRefreshPageCache = true;
							continue;
						}
					}

					if (shouldDecrypt)
					{
						MemoryMarshal.Cast<byte, uint>(tweakInitializationVector)[0] =
							MemoryMarshal.Cast<byte, uint>(currentHashEntry.Slice(_hashEntryLength, sizeof(uint)))[0];
					}

					hashCacheOffset += (int)XvdFile.HASH_ENTRY_LENGTH;
					hashCacheEntryIndex++;
					if (hashCacheEntryIndex == XvdFile.HASH_ENTRIES_IN_PAGE)
					{
						hashCacheEntryIndex = 0;
						hashCacheOffset += 0x10; // Alignment for page boundaries (0xff0 -> 0x1000)
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
					decryptor.Transform(currentPageData, currentPageData, tweakInitializationVector);
				}

				outputFileStream.Write(currentPageData[..currentChunkSize]);

				remainingSegmentSize -= (uint)currentChunkSize;

				pageCacheOffset += (int)XvdFile.PAGE_SIZE;
				if (pageCacheOffset == pageCache.Length)
				{
					totalPageCacheOffset += pageCacheOffset;
					pageCacheOffset = 0;
					shouldRefreshPageCache = true;
				}

				processedPageCount++;
				progressTask.Increment(XvdFile.PAGE_SIZE);
			} while (remainingSegmentSize > 0);

			currentSegmentIndex++;
		}
	}
}