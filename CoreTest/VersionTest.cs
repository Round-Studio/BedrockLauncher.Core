using System;
using System.Collections.Generic;
using System.Text;
using BedrockLauncher.Core.Utils;

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
				
				Console.WriteLine(VersionHelper.GetNextVersion(new Version("1.21.10006.0")));
			}
       	}	
	}
}
