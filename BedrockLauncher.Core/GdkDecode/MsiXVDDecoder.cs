using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BedrockLauncher.Core.GdkDecode;

public class MsiXVDDecoder
{
	public KeySinagl d;
	public KeySinagl t;

	public MsiXVDDecoder(CikKey key)
	{
		d.Init(key.DKey,true);
		t.Init(key.TKey,false);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> Gf128Mul(Vector128<byte> iv, Vector128<byte> mask)
	{
		Vector128<byte> tmp1 = Sse2.Add(iv.AsUInt64(), iv.AsUInt64()).AsByte();
		Vector128<byte> tmp2 = Sse2.Shuffle(iv.AsInt32(), 0x13).AsByte();
		tmp2 = Sse2.ShiftRightArithmetic(tmp2.AsInt32(), 31).AsByte();
		tmp2 = Sse2.And(mask, tmp2);
		return Sse2.Xor(tmp1, tmp2);
	}

	public int Decrypt(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> tweakIv)
	{
		if (tweakIv.Length < 16)
			return 0;

		var iv = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(tweakIv));

		int length = Math.Min(input.Length, output.Length);
		if (length == 0)
			return 0;

		int remainingBlocks = length >> 4;
		int leftover = length & 0xF;

		if (leftover != 0)
			remainingBlocks--;

		if (remainingBlocks <= 0 && leftover == 0)
			return 0;

		ref Vector128<byte> inBlock = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(input));
		ref Vector128<byte> outBlock = ref Unsafe.As<byte, Vector128<byte>>(ref MemoryMarshal.GetReference(output));

		Vector128<byte> mask = Vector128.Create(0x87, 1).AsByte();
		Vector128<byte> tweak = t.EncryptUnrolled(iv);

		while (remainingBlocks > 7)
		{
			Vector128<byte> tweak1 = Gf128Mul(tweak, mask);
			Vector128<byte> tweak2 = Gf128Mul(tweak1, mask);
			Vector128<byte> tweak3 = Gf128Mul(tweak2, mask);
			Vector128<byte> tweak4 = Gf128Mul(tweak3, mask);
			Vector128<byte> tweak5 = Gf128Mul(tweak4, mask);
			Vector128<byte> tweak6 = Gf128Mul(tweak5, mask);
			Vector128<byte> tweak7 = Gf128Mul(tweak6, mask);

			Vector128<byte> b0 = Sse2.Xor(tweak, Unsafe.Add(ref inBlock, 0));
			Vector128<byte> b1 = Sse2.Xor(tweak1, Unsafe.Add(ref inBlock, 1));
			Vector128<byte> b2 = Sse2.Xor(tweak2, Unsafe.Add(ref inBlock, 2));
			Vector128<byte> b3 = Sse2.Xor(tweak3, Unsafe.Add(ref inBlock, 3));
			Vector128<byte> b4 = Sse2.Xor(tweak4, Unsafe.Add(ref inBlock, 4));
			Vector128<byte> b5 = Sse2.Xor(tweak5, Unsafe.Add(ref inBlock, 5));
			Vector128<byte> b6 = Sse2.Xor(tweak6, Unsafe.Add(ref inBlock, 6));
			Vector128<byte> b7 = Sse2.Xor(tweak7, Unsafe.Add(ref inBlock, 7));

			DecryptBlocks8(b0, b1, b2, b3, b4, b5, b6, b7,
				out b0, out b1, out b2, out b3, out b4, out b5, out b6, out b7);

			Unsafe.Add(ref outBlock, 0) = Sse2.Xor(tweak, b0);
			Unsafe.Add(ref outBlock, 1) = Sse2.Xor(tweak1, b1);
			Unsafe.Add(ref outBlock, 2) = Sse2.Xor(tweak2, b2);
			Unsafe.Add(ref outBlock, 3) = Sse2.Xor(tweak3, b3);
			Unsafe.Add(ref outBlock, 4) = Sse2.Xor(tweak4, b4);
			Unsafe.Add(ref outBlock, 5) = Sse2.Xor(tweak5, b5);
			Unsafe.Add(ref outBlock, 6) = Sse2.Xor(tweak6, b6);
			Unsafe.Add(ref outBlock, 7) = Sse2.Xor(tweak7, b7);

			tweak = Gf128Mul(tweak7, mask);
			inBlock = ref Unsafe.Add(ref inBlock, 8);
			outBlock = ref Unsafe.Add(ref outBlock, 8);
			remainingBlocks -= 8;
		}

		while (remainingBlocks > 0)
		{
			Vector128<byte> tmp = Sse2.Xor(inBlock, tweak);
			tmp = d.DecryptBlockUnrolled(tmp);
			outBlock = Sse2.Xor(tmp, tweak);

			tweak = Gf128Mul(tweak, mask);
			inBlock = ref Unsafe.Add(ref inBlock, 1);
			outBlock = ref Unsafe.Add(ref outBlock, 1);
			remainingBlocks--;
		}

		if (leftover != 0)
		{
			Vector128<byte> finalTweak = Gf128Mul(tweak, mask);

			Vector128<byte> tmp = Sse2.Xor(inBlock, finalTweak);
			tmp = d.DecryptBlockUnrolled(tmp);
			outBlock = Sse2.Xor(tmp, finalTweak);

			Span<byte> currentOutBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref outBlock, 1));
			Span<byte> nextInBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref inBlock, 1), 1));
			Span<byte> nextOutBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref outBlock, 1), 1));

			Span<byte> temp = stackalloc byte[16];

			for (int i = 0; i < leftover; i++)
			{
				nextOutBytes[i] = currentOutBytes[i];
				temp[i] = nextInBytes[i];
			}

			for (int i = leftover; i < 16; i++)
			{
				temp[i] = currentOutBytes[i];
			}

			tmp = Unsafe.ReadUnaligned<Vector128<byte>>(ref temp[0]);
			tmp = Sse2.Xor(tmp, tweak);
			tmp = d.DecryptBlockUnrolled(tmp);
			outBlock = Sse2.Xor(tmp, tweak);
		}

		return length;
	}
	
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public  void DecryptBlocks8(
		Vector128<byte> in0,
		Vector128<byte> in1,
		Vector128<byte> in2,
		Vector128<byte> in3,
		Vector128<byte> in4,
		Vector128<byte> in5,
		Vector128<byte> in6,
		Vector128<byte> in7,
		out Vector128<byte> out0,
		out Vector128<byte> out1,
		out Vector128<byte> out2,
		out Vector128<byte> out3,
		out Vector128<byte> out4,
		out Vector128<byte> out5,
		out Vector128<byte> out6,
		out Vector128<byte> out7)
	{
		ReadOnlySpan<Vector128<byte>> keys = d.RKeys;

		Vector128<byte> key = keys[10];
		Vector128<byte> b0 = Sse2.Xor(in0, key);
		Vector128<byte> b1 = Sse2.Xor(in1, key);
		Vector128<byte> b2 = Sse2.Xor(in2, key);
		Vector128<byte> b3 = Sse2.Xor(in3, key);
		Vector128<byte> b4 = Sse2.Xor(in4, key);
		Vector128<byte> b5 = Sse2.Xor(in5, key);
		Vector128<byte> b6 = Sse2.Xor(in6, key);
		Vector128<byte> b7 = Sse2.Xor(in7, key);

		key = keys[9];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[8];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[7];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[6];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[5];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[4];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[3];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[2];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[1];
		b0 = Aes.Decrypt(b0, key);
		b1 = Aes.Decrypt(b1, key);
		b2 = Aes.Decrypt(b2, key);
		b3 = Aes.Decrypt(b3, key);
		b4 = Aes.Decrypt(b4, key);
		b5 = Aes.Decrypt(b5, key);
		b6 = Aes.Decrypt(b6, key);
		b7 = Aes.Decrypt(b7, key);

		key = keys[0];
		out0 = Aes.DecryptLast(b0, key);
		out1 = Aes.DecryptLast(b1, key);
		out2 = Aes.DecryptLast(b2, key);
		out3 = Aes.DecryptLast(b3, key);
		out4 = Aes.DecryptLast(b4, key);
		out5 = Aes.DecryptLast(b5, key);
		out6 = Aes.DecryptLast(b6, key);
		out7 = Aes.DecryptLast(b7, key);
	}

}