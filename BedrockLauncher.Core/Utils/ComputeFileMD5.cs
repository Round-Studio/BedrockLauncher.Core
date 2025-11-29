using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BedrockLauncher.Core.Utils
{
	public class ComputeFileMD5
	{
		public static async Task<string> ComputeFileMD5Async(string filePath)
		{
			const int bufferSize = 8192 * 16;

			using (var md5 = MD5.Create())
			using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous))
			{
				byte[] buffer = new byte[bufferSize];
				int bytesRead;
				long totalBytesRead = 0;

				while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					if (bytesRead == buffer.Length)
					{
						md5.TransformBlock(buffer, 0, bytesRead, null, 0);
					}
					else
					{
						md5.TransformFinalBlock(buffer, 0, bytesRead);
					}
					totalBytesRead += bytesRead;
				}

				if (totalBytesRead == 0 || md5.Hash == null)
				{
					md5.TransformFinalBlock(buffer, 0, 0);
				}

				return BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
			}
		}
	}
}
