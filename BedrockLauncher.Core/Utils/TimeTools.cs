using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockLauncher.Core.Utils
{
	public static class TimeBasedVersion
	{
		/// <summary>
		/// Returns a version string based on the current date and time.
		/// </summary>
		/// <remarks>The returned version string reflects the exact date and time at which the method is called. This
		/// can be useful for generating unique version identifiers or timestamps.</remarks>
		/// <returns>A string in the format "yyyy.M.d.mmm", where yyyy is the year, M is the month, d is the day, and mmm is the total
		/// number of minutes since midnight.</returns>
		public static string GetVersion()
		{
			DateTime now = DateTime.Now;

			int year = now.Year;
			int month = now.Month;      
			int day = now.Day;  
			int totalMinutes = now.Hour * 60 + now.Minute;  

			return $"{year}.{month}.{day}.{totalMinutes}";
		}
		/// <summary>
		/// Creates a Version object representing the current date and time.
		/// </summary>
		/// <remarks>The returned Version object encodes the current date and time as follows: major is the year,
		/// minor is the month, build is the day, and revision is the total number of minutes since midnight. This can be used
		/// to uniquely identify the current moment within a day.</remarks>
		/// <returns>A Version object where the major, minor, build, and revision components correspond to the current year, month,
		/// day, and total minutes past midnight, respectively.</returns>
		public static Version GetVersionObject()
		{
			DateTime now = DateTime.Now;
			int year = now.Year;
			int month = now.Month;
			int day = now.Day;
			int totalMinutes = now.Hour * 60 + now.Minute;
			return new Version(year, month, day, totalMinutes);
		}
		[UnmanagedCallersOnly(EntryPoint = "Add")]
		public static void GetVersionString() { }
	}
}
