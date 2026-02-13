using System;
using System.Collections.Generic;
using System.Text;
using BedrockLauncher.Core.Utils;
using BedrockLauncher.Core.VersionJsons;

namespace CoreTest
{
	[TestClass]
	public class VersionTest
	{
		

		[TestMethod]
		public void Test()
		{
			

			for (long i = 0; i < 1000; i++)
			{
				
				Console.WriteLine(VersionsHelper.GetNextVersion(new Version("1.21.11236.0")));
			}
       	}	
	}
}
