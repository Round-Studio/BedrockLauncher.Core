using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BedrockLauncher.Core.GdkDecode;

internal static class Extensions
{
	public static unsafe T GetstructFromBytes<T>(ReadOnlySpan<byte> bytes) where T : struct
	{
		var size = Marshal.SizeOf<T>();
		if (bytes.Length < size)
			throw new ArgumentException("Bytes out of length");

		fixed (byte* ptr = bytes)
		{
			return Marshal.PtrToStructure<T>((IntPtr)ptr);
		}
		
	}
	public static unsafe T[] GetstructArraysFromBytes<T>(ReadOnlySpan<byte> bytes,long counts) where T : struct
	{
		var size = Marshal.SizeOf(typeof(T));
		if (bytes.Length < size*counts)
			throw new ArgumentException("Bytes out of length");

		fixed (byte* ptr = bytes)
		{
			var TAry = new T[counts];
			for (int i = 0; i < counts; i++)
			{
				TAry[i] = Marshal.PtrToStructure<T>((IntPtr)ptr+size*i);
			}
			return TAry;
		}
	}
	public static ulong GetPageOffset(ulong soureUlong)
	{
		return soureUlong / 0x1000;
	}
	public static ulong PageToOffset(ulong soureUlong)
	{
		return soureUlong * 0x1000;
	}
	public static ulong BytesToPages(ulong bytes)
	{
		return (bytes + 0x1000 - 1) / 0x1000;
	}
	public static ulong ComputeHashBlockIndexForDataBlock(MsiXVDKind imageType, ulong hashTreeDepth, ulong totalHashedPages,
		ulong dataBlockIndex, uint currentHashLevel, out ulong entryIndexInHashBlock,
		bool isResilient = false, bool isUnknown = false)
	{
		ulong ComputeLevelMultiplier(ulong levelCount)
		{
			return (ulong)Math.Pow(0xAA, levelCount);
		}

		ulong hashBlockIndex = 0xFFFF;
		entryIndexInHashBlock = 0;

		if ((uint)imageType > 1 || currentHashLevel > 3)
			return hashBlockIndex; // Invalid parameters

		if (currentHashLevel == 0)
			entryIndexInHashBlock = dataBlockIndex % 0xAA;
		else
			entryIndexInHashBlock = dataBlockIndex / ComputeLevelMultiplier(currentHashLevel) % 0xAA;

		if (currentHashLevel == 3)
			return 0;

		hashBlockIndex = dataBlockIndex / ComputeLevelMultiplier(currentHashLevel + 1);
		hashTreeDepth -= currentHashLevel + 1;

		if (currentHashLevel == 0 && hashTreeDepth > 0)
		{
			hashBlockIndex += (totalHashedPages + ComputeLevelMultiplier(2) - 1) / ComputeLevelMultiplier(2);
			hashTreeDepth--;
		}

		if ((currentHashLevel == 0 || currentHashLevel == 1) && hashTreeDepth > 0)
		{
			hashBlockIndex += (totalHashedPages + ComputeLevelMultiplier(3) - 1) / ComputeLevelMultiplier(3);
			hashTreeDepth--;
		}

		if (hashTreeDepth > 0)
			hashBlockIndex += (totalHashedPages + ComputeLevelMultiplier(4) - 1) / ComputeLevelMultiplier(4);

		if (isResilient)
			hashBlockIndex *= 2;
		if (isUnknown)
			hashBlockIndex++;

		return hashBlockIndex;
	}
}