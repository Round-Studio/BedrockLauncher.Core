using System;
using System.Collections.Generic;
using System.Text;

namespace BedrockLauncher.Core.Utils
{
	/// <summary>
	/// Represents progress information for a decompression operation, including the file name and item counts.
	/// </summary>
	public struct DecompressProgress
	{
		/// <summary>
		/// FileName
		/// </summary>
		public string FileName;
		/// <summary>
		/// Current Count
		/// </summary>
		public long CurrentCount;
		/// <summary>
		/// Represents the total number of items counted.
		/// </summary>
		public long TotalCount;
		public double Percentage => TotalCount > 0 ? (double)CurrentCount / TotalCount * 100 : 0;
	}
}
