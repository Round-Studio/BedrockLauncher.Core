using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BedrockLauncher.Core.GdkDecode;

public class MsiXVDDecoder
{
	private Vector128<byte> dkeysVector128;
	private Vector128<byte> tkeysVector128;
	public ref readonly ReadOnlySpan<Vector128<byte>> RDKeys
	{
		get
		{
			ref var start = ref Unsafe.AsRef(in dkeysVector128);
			var span = MemoryMarshal.CreateReadOnlySpan(ref start, 11);
			return ref Unsafe.AsRef(in span);
		}
	}
	public ref readonly ReadOnlySpan<Vector128<byte>> RTKeys
	{
		get
		{
			ref var start = ref Unsafe.AsRef(in tkeysVector128);
			var span = MemoryMarshal.CreateReadOnlySpan(ref start, 11);
			return ref Unsafe.AsRef(in span);
		}
	}

	public MsiXVDDecoder(in CikKey key)
	{
		InitKey(key.DKey,ref dkeysVector128,true);
		InitKey(key.TKey,ref tkeysVector128,false);

	}
	private void InitKey(ReadOnlySpan<byte> bytes,ref Vector128<byte> keyVector128,bool isDecrypt)
	{
		Span<Vector128<byte>> Keys = MemoryMarshal.CreateSpan(ref keyVector128, 11);
		var curKey = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(bytes));
		Keys[0] = curKey;

		ReadOnlySpan<byte> rcon = [0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36];

		for (int i = 0; i < 10; i++)
		{
			curKey = KeyExpansionCore(curKey, Aes.KeygenAssist(curKey, rcon[i]));
			Keys[i + 1] = curKey;
		}

		if (isDecrypt)
		{
			ApplyInverseMixColumns(Keys);
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> KeyExpansionCore(Vector128<byte> curk, Vector128<byte> recon)
	{
		var result = curk;
		var rotated = Sse2.Shuffle(recon.AsUInt32(), 0xFF).AsByte();

		result = Sse2.Xor(result, Sse2.ShiftLeftLogical128BitLane(result, 4));
		result = Sse2.Xor(result, Sse2.ShiftLeftLogical128BitLane(result, 8));

		return Sse2.Xor(result, rotated);
	}

	private static void ApplyInverseMixColumns(Span<Vector128<byte>> roundKeys)
	{
		for (int i = 1; i <= 9; i++)
		{
			roundKeys[i] = Aes.InverseMixColumns(roundKeys[i]);
		}
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
		Vector128<byte> tweak = EncryptUnrolled(iv,RTKeys);

		// 处理8个块一组的批量解密
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

		// 处理剩余的单块
		while (remainingBlocks > 0)
		{
			Vector128<byte> tmp = Sse2.Xor(inBlock, tweak);
			tmp = DecryptBlockUnrolled(tmp, RDKeys);
			outBlock = Sse2.Xor(tmp, tweak);

			tweak = Gf128Mul(tweak, mask);
			inBlock = ref Unsafe.Add(ref inBlock, 1);
			outBlock = ref Unsafe.Add(ref outBlock, 1);
			remainingBlocks--;
		}

		// 处理部分块（如果存在）- 移除 Buffer16
		if (leftover != 0)
		{
			Vector128<byte> finalTweak = Gf128Mul(tweak, mask);

			Vector128<byte> tmp = Sse2.Xor(inBlock, finalTweak);
			tmp = DecryptBlockUnrolled(tmp,RDKeys);
			outBlock = Sse2.Xor(tmp, finalTweak);

			// 直接使用字节Span操作，移除Buffer16
			Span<byte> currentOutBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref outBlock, 1));
			Span<byte> nextInBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref inBlock, 1), 1));
			Span<byte> nextOutBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref outBlock, 1), 1));

			// 使用stackalloc创建临时缓冲区
			Span<byte> temp = stackalloc byte[16];

			// 复制部分数据
			for (int i = 0; i < leftover; i++)
			{
				nextOutBytes[i] = currentOutBytes[i];
				temp[i] = nextInBytes[i];
			}

			// 填充剩余字节
			for (int i = leftover; i < 16; i++)
			{
				temp[i] = currentOutBytes[i];
			}

			// 处理临时数据
			tmp = Unsafe.ReadUnaligned<Vector128<byte>>(ref temp[0]);
			tmp = Sse2.Xor(tmp, tweak);
			tmp = DecryptBlockUnrolled(tmp,RDKeys);
			outBlock = Sse2.Xor(tmp, tweak);
		}

		return length;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector128<byte> EncryptUnrolled(Vector128<byte> input,ReadOnlySpan<Vector128<byte>> keysvVector128)
	{
		ReadOnlySpan<Vector128<byte>> keys = keysvVector128;

		Vector128<byte> state = Sse2.Xor(input, keys[0]);
		state = Aes.Encrypt(state, keys[1]);
		state = Aes.Encrypt(state, keys[2]);
		state = Aes.Encrypt(state, keys[3]);
		state = Aes.Encrypt(state, keys[4]);
		state = Aes.Encrypt(state, keys[5]);
		state = Aes.Encrypt(state, keys[6]);
		state = Aes.Encrypt(state, keys[7]);
		state = Aes.Encrypt(state, keys[8]);
		state = Aes.Encrypt(state, keys[9]);
		return Aes.EncryptLast(state, keys[10]);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public  Vector128<byte> DecryptBlockUnrolled(Vector128<byte> input,ReadOnlySpan<Vector128<byte>> keysvVector128)
	{
		ReadOnlySpan<Vector128<byte>> keys = keysvVector128;

		Vector128<byte> state = Sse2.Xor(input, keys[10]);
		state = Aes.Decrypt(state, keys[9]);
		state = Aes.Decrypt(state, keys[8]);
		state = Aes.Decrypt(state, keys[7]);
		state = Aes.Decrypt(state, keys[6]);
		state = Aes.Decrypt(state, keys[5]);
		state = Aes.Decrypt(state, keys[4]);
		state = Aes.Decrypt(state, keys[3]);
		state = Aes.Decrypt(state, keys[2]);
		state = Aes.Decrypt(state, keys[1]);
		return Aes.DecryptLast(state, keys[0]);
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
		ReadOnlySpan<Vector128<byte>> keys = RDKeys;

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