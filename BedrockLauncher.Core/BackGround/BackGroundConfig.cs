using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BedrockLauncher.Core.BackGround
{
	/// <summary>
	/// Represents the configuration settings for a background, including the file path and optional background color.
	/// This function will not be used when you launch a gdk version minecraft
	/// </summary>
	public struct BackGroundConfig
	{
		/// <summary>
		/// Gets or sets the full path of the file, including the directory and file name.
		/// </summary>
		public string FileFullPath;
		/// <summary>
		/// Specifies the background color to use, or null to indicate no background color is set.
		/// </summary>
		public Color? BackGroundColor;
	}
}
