using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
namespace BedrockLauncher.Core.Network
{
    public struct MsStoreUri
    {
        public static Uri cookieUri      = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        public static Uri fileListXmlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        public static Uri  urlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured");
    }
    public static class ProductorHelper
    {
        
        public static string GetCookie(HttpClient client)
        {
            StringContent stringContent = new(DEFINE.COOKIE);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml");
            stringContent.Headers.Expires = DateTime.Now;
            stringContent.Headers.ContentLength = DEFINE.COOKIE.Length;
            stringContent.Headers.ContentType.CharSet = "utf-8";
            HttpResponseMessage result = client.PostAsync(MsStoreUri.cookieUri,stringContent).Result;
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception("GetMsStoreCookieError");
            }
            Dictionary<string, string> responseDict = new()
            {
                { "Status code", Convert.ToString(result.StatusCode) },
                { "Headers", result.Headers is null ? string.Empty : Convert.ToString(result.Headers).Replace('\r', ' ').Replace('\n', ' ') },
                { "Response message:", result.RequestMessage is null ? string.Empty : Convert.ToString(result.RequestMessage).Replace('\r', ' ').Replace('\n', ' ') }
            };
            string responseString = result.Content.ReadAsStringAsync().Result;
            if (!string.IsNullOrEmpty(responseString))
            {
                XmlDocument responseStringDocument = new();
                responseStringDocument.LoadXml(responseString);

                XmlNodeList encryptedDataList = responseStringDocument.GetElementsByTagName("EncryptedData");
                if (encryptedDataList.Count > 0)
                {
                    return encryptedDataList[0].InnerText;
                }
            }
            throw new Exception("HandleMsStoreCookieError");
        }
    }
}
