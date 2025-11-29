open System.IO

    let content = """namespace BedrockLauncher.Core
{
internal struct _DEFINE_REF2
{
	public static readonly byte[] pre = new byte[]{0x00};
	public static readonly byte[] rel = new byte[]{0x00};
}
}"""

 let filename = "_DEFINE_REF2.ARGS_KEY_AES_..RS54.cs"

try
     File.WriteAllText(filename, content)
     printfn "Generate: %s" filename
  with
  | ex -> printfn "Failed to Generate: %s" ex.Message