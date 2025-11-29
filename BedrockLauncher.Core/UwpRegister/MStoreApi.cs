using System;
using System.Collections.Generic;
using System.Text;

namespace BedrockLauncher.Core.UwpRegister
{
	public struct MStoreUri
	{
		public static Uri cookieUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
		public static Uri fileListXmlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
		public static Uri updateUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured");
		public static Uri productUri = new("https://storeedgefd.dsx.mp.microsoft.com/v9.0/products/9NBLGGH2JHXJ?market=US&locale=en-US&deviceFamily=Windows.Desktop");
	}
}
