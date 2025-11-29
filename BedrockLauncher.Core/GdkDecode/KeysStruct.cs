using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace BedrockLauncher.Core.GdkDecode
{
	[StructLayout(LayoutKind.Sequential, Size = 11 * 0x10)]
	public struct KeySinagl
	{
		public Vector128<byte> Keys;
		public readonly ReadOnlySpan<Vector128<byte>> RKeys =>
			MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in Keys), 11);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector128<byte> KeyExpansion(Vector128<byte> s, Vector128<byte> t)
		{
			t = Sse2.Shuffle(t.AsUInt32(), 0xFF).AsByte();
			s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 4));
			s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 8));

			return Sse2.Xor(s, t);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector128<byte> DecryptBlockUnrolled(Vector128<byte> input)
		{
			ReadOnlySpan<Vector128<byte>> keys = RKeys;

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
		public Vector128<byte> EncryptUnrolled(Vector128<byte> input)
		{
			ReadOnlySpan<Vector128<byte>> keys = RKeys;

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
		public void Init(ReadOnlySpan<byte> keyBytes, bool isDecryption)
		{
			const int keySize = 16;
			const int rounds = 10;

			if (keyBytes.Length < keySize)
				throw new ArgumentException($"Key Length is not enough", nameof(keyBytes));

			Span<Vector128<byte>> roundKeys = MemoryMarshal.CreateSpan(ref Keys, rounds + 1);

			ReadOnlySpan<byte> rconConstants = [0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36];

			roundKeys[0] = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(keyBytes));
			for (int round = 0; round < rounds; round++)
			{
				roundKeys[round + 1] = KeyExpansion(
					roundKeys[round],
					Aes.KeygenAssist(roundKeys[round], rconConstants[round])
				);
			}
			if (isDecryption)
			{
				for (int i = 1; i < rounds; i++)
				{
					roundKeys[i] = Aes.InverseMixColumns(roundKeys[i]);
				}
			}
		}
	} 
}
