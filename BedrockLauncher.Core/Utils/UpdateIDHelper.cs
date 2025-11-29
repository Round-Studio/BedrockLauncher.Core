using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BedrockLauncher.Core.UwpRegister;

namespace BedrockLauncher.Core.Utils
{
	public static class UpdateIDHelper
	{
		/// <summary>
		/// Gets the download URI for the specified update ID by sending a SOAP request and parsing the response.
		/// </summary>
		/// <param name="updateId">The update identifier</param>
		/// <returns>Download URI string</returns>
		/// <exception cref="WebException">Thrown when network request fails</exception>
		/// <exception cref="XmlException">Thrown when XML processing fails</exception>
		public static async Task<string> GetUriAsync(string updateId)
		{
			// Prepare SOAP request with update details
			DateTime now = DateTime.UtcNow;
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(SoapApi.FE3FileUrl);

			xmlDoc.GetElementsByTagName("UpdateID")[0].InnerText = updateId;
			xmlDoc.GetElementsByTagName("Created")[0].InnerText = now.ToString("o");
			xmlDoc.GetElementsByTagName("Expires")[0].InnerText = now.AddMinutes(5).ToString("o");
			xmlDoc.GetElementsByTagName("deviceAttributes")[0].InnerText = SoapApi.DeviceAttributes;

			string soapRequest = xmlDoc.InnerXml;

			// Send SOAP request asynchronously
			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type",SoapApi.SoapContentType);

				using (var content = new StringContent(soapRequest, Encoding.UTF8, "application/soap+xml"))
				using (var response = await client.PostAsync(MStoreUri.updateUri.OriginalString, content))
				{
					response.EnsureSuccessStatusCode();
					string soapResponse = await response.Content.ReadAsStringAsync();

					// Parse response to extract download URL
					XDocument xdoc = XDocument.Parse(soapResponse);
					XNamespace ns = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService";

					var urls = xdoc.Descendants(ns + "Url")
						.Select(x => WebUtility.HtmlDecode(x.Value))
						.ToList();

					// Identify complex URL based on specific patterns
					var complexUrl = urls.FirstOrDefault(url =>
						url.Contains("?P1=") ||
						url.Contains("tlu.dl.") ||
						url.Contains("&P2=") ||
						url.Contains("%3d") ||
						url.Length > 150);

					return complexUrl ?? urls.LastOrDefault();
				}
			}
		}
	}
}
