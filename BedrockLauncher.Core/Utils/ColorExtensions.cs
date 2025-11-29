using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BedrockLauncher.Core.Utils
{
	public static class ColorExtensions
	{
		public static string ToHex(this Color color)
		{
			return $"{color.R:X2}{color.G:X2}{color.B:X2}";
		}

		public static string ToHex(this Color color, bool includeHash = false)
		{
			string hex = $"{color.R:X2}{color.G:X2}{color.B:X2}";
			return includeHash ? "#" + hex : hex;
		}
	}
}
