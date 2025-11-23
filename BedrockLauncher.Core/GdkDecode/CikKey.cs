namespace BedrockLauncher.Core.GdkDecode;

public readonly struct CikKey
{
	public const byte MaxSize = 0x30;
	public readonly Guid Guid;
	public readonly byte[] TKey;
	public readonly byte[] DKey;

	public CikKey(ReadOnlySpan<byte> cik)
	{
		this.Guid = new Guid(cik[..0x10]);
		TKey = cik[0x10..0x20].ToArray();
		DKey = cik[0x20..].ToArray();
	}
	

	public CikKey(string hexString)
	{
		var cik = Convert.FromHexString(hexString);
		this.Guid = new Guid(cik[..0x10]);
		TKey = cik[0x10..0x20].ToArray();
		DKey = cik[0x20..].ToArray();
	}
}