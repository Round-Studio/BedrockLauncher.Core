open System
open System.IO

// Get environment variable and convert to byte array
let getKeyBytes (envVarName: string) =
    let envValue = Environment.GetEnvironmentVariable(envVarName)
    
    if String.IsNullOrEmpty(envValue) then
        failwithf "Environment variable %s is not set or empty" envVarName
    
    // Remove possible 0x prefix and spaces
    let cleanHex = envValue.Replace("0x", "").Replace(" ", "").ToUpper()
    
    // Validate hex string format
    if cleanHex.Length % 2 <> 0 then
        failwithf "Environment variable %s has incorrect hex string length: %s" envVarName envValue
    
    if not <| System.Text.RegularExpressions.Regex.IsMatch(cleanHex, "^[0-9A-F]+$") then
        failwithf "Environment variable %s contains invalid hex characters: %s" envVarName envValue
    
    // Convert hex string to byte array
    try
        [| for i in 0..2..(cleanHex.Length - 1) -> 
            Convert.ToByte(cleanHex.Substring(i, 2), 16) |]
    with
    | ex -> failwithf "Failed to convert environment variable %s: %s" envVarName ex.Message

// Generate C# code representation of byte array
let generateByteArrayCode (bytes: byte[]) =
    if bytes.Length = 0 then "new byte[]{0x00}"
    else
        let hexStrings = bytes |> Array.map (fun b -> sprintf "0x%02X" b)
        sprintf "new byte[]{%s}" (String.Join(",", hexStrings))

// Generate C# file content
let generateCSharpCode (preBytes: byte[]) (relBytes: byte[]) =
    let preCode = generateByteArrayCode preBytes
    let relCode = generateByteArrayCode relBytes
    
    sprintf """namespace BedrockLauncher.Core
{
    internal struct _DEFINE_REF2
    {
        public static readonly byte[] pre = %s;
        public static readonly byte[] rel = %s;
    }
}""" preCode relCode

// Main execution function
try
    printfn "Starting to read environment variables and generate C# code..."
    
    // Read environment variables
    let preBytes = getKeyBytes "PRE_MC_KEY"
    let relBytes = getKeyBytes "REL_MC_KEY"
    
    printfn "PRE_MC_KEY byte count: %d" preBytes.Length
    printfn "REL_MC_KEY byte count: %d" relBytes.Length
    
    // Generate C# code
    let csharpCode = generateCSharpCode preBytes relBytes
    
    // Write to file
    let outputPath = Path.Combine(Directory.GetCurrentDirectory(), "_DEFINE_REF2.ARGS_KEY_AES_..RS54.cs")
    File.WriteAllText(outputPath, csharpCode)
    
    printfn "File generated: %s" outputPath
    printfn "Generation successful!"
    
with
| ex -> 
    printfn "Error: %s" ex.Message
    Environment.Exit(1)