using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using CryptAes = System.Security.Cryptography.Aes;
using SysCrypt = System.Security.Cryptography;

namespace BedrockLauncher.Core.GdkDecode;

public class MsiXVDDecoder : IDisposable
{
	public KeySinagl d;
	public KeySinagl t;
	private readonly bool _useHardware;
	private readonly CryptAes? _swAesD;
	private readonly CryptAes? _swAesT;

	public MsiXVDDecoder(CikKey key) : this(key, true)
	{
	}

	public MsiXVDDecoder(CikKey key, bool useHardware)
	{
		_useHardware = useHardware;
		if (useHardware)
		{
			d.Init(key.DKey, true);
			t.Init(key.TKey, false);
			_swAesD = null;
			_swAesT = null;
		}
		else
		{
			_swAesD = CryptAes.Create();
			_swAesD.KeySize = 128;
			_swAesD.Key = key.DKey;
			_swAesD.Mode = SysCrypt.CipherMode.ECB;
			_swAesD.Padding = SysCrypt.PaddingMode.None;

			_swAesT = CryptAes.Create();
			_swAesT.KeySize = 128;
			_swAesT.Key = key.TKey;
			_swAesT.Mode = SysCrypt.CipherMode.ECB;
			_swAesT.Padding = SysCrypt.PaddingMode.None;
		}
	}

	public int Decrypt(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> tweakIv)
	{
		if (_useHardware)
			return DecryptHardware(input, output, tweakIv);
		else
			return DecryptSoftware(input, output, tweakIv);
	}

	#region Hardware Decode (AES-NI / SSE2)

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> Gf128Mul(Vector128<byte> iv, Vector128<byte> mask)
	{
		Vector128<byte> tmp1 = Sse2.Add(iv.AsUInt64(), iv.AsUInt64()).AsByte();
		Vector128<byte> tmp2 = Sse2.Shuffle(iv.AsInt32(), 0x13).AsByte();
		tmp2 = Sse2.ShiftRightArithmetic(tmp2.AsInt32(), 31).AsByte();
		tmp2 = Sse2.And(mask, tmp2);
		return Sse2.Xor(tmp1, tmp2);
	}

	private int DecryptHardware(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> tweakIv)
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
	public void DecryptBlocks8(
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

	#endregion

	#region Software Decode (managed AES)

	private int DecryptSoftware(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> tweakIv)
	{
		if (tweakIv.Length < 16)
			return 0;

		int length = Math.Min(input.Length, output.Length);
		if (length == 0)
			return 0;

		int remainingBlocks = length >> 4;
		int leftover = length & 0xF;

		if (leftover != 0)
			remainingBlocks--;

		if (remainingBlocks <= 0 && leftover == 0)
			return 0;

		byte[] tweak = new byte[16];
		_swAesT!.EncryptEcb(tweakIv[..16], tweak, SysCrypt.PaddingMode.None);

		Span<byte> tempIn = stackalloc byte[16];
		Span<byte> tempOut = stackalloc byte[16];
		int offset = 0;

		while (remainingBlocks > 0)
		{
			XorBytes(input.Slice(offset, 16), tweak, tempIn);
			_swAesD.DecryptEcb(tempIn, tempOut, SysCrypt.PaddingMode.None);
			XorBytes(tempOut, tweak, output.Slice(offset, 16));
			Gf128MulSoftware(tweak);
			offset += 16;
			remainingBlocks--;
		}

		if (leftover != 0)
		{
			byte[] finalTweak = tweak[..]; // copy: finalTweak = Gf128Mul(tweak)
			Gf128MulSoftware(finalTweak);

			XorBytes(input.Slice(offset, 16), finalTweak, tempIn);
			_swAesD.DecryptEcb(tempIn, tempOut, SysCrypt.PaddingMode.None);
			XorBytes(tempOut, finalTweak, output.Slice(offset, 16));

			Span<byte> currentOut = output.Slice(offset, 16);
			ReadOnlySpan<byte> nextIn = input.Slice(offset + 16, leftover);
			Span<byte> nextOut = output.Slice(offset + 16, leftover);

			Span<byte> temp = stackalloc byte[16];

			for (int i = 0; i < leftover; i++)
			{
				nextOut[i] = currentOut[i];
				temp[i] = nextIn[i];
			}

			for (int i = leftover; i < 16; i++)
			{
				temp[i] = currentOut[i];
			}

			XorBytes(temp, tweak, tempIn);
			_swAesD.DecryptEcb(tempIn, tempOut, SysCrypt.PaddingMode.None);
			XorBytes(tempOut, tweak, output.Slice(offset, 16));
		}

		return length;
	}

	private static void XorBytes(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> result)
	{
		for (int i = 0; i < 16; i++)
			result[i] = (byte)(a[i] ^ b[i]);
	}

	private static void Gf128MulSoftware(Span<byte> iv)
	{
		int carryBit127 = (iv[15] >> 7) & 1;
		int carryBit63 = (iv[7] >> 7) & 1;

		for (int i = 7; i > 0; i--)
			iv[i] = (byte)((iv[i] << 1) | (iv[i - 1] >> 7));
		iv[0] <<= 1;

		for (int i = 15; i > 8; i--)
			iv[i] = (byte)((iv[i] << 1) | (iv[i - 1] >> 7));
		iv[8] <<= 1;

		if (carryBit63 != 0) iv[8] ^= 0x01;
		if (carryBit127 != 0) iv[0] ^= 0x87;
	}

	#endregion

	public void Dispose()
	{
		_swAesD?.Dispose();
		_swAesT?.Dispose();
		GC.SuppressFinalize(this);
	}
}