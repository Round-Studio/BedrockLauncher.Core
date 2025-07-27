using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
namespace BedrockLauncher.Core.Network
{
    
    public static class ProductorHelper
    {
        public static string GetCookie(HttpClient client)
        {
            StringContent stringContent = new(DEFINE.COOKIE);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml");
            stringContent.Headers.Expires = DateTime.Now;
            stringContent.Headers.ContentLength = DEFINE.COOKIE.Length;
            stringContent.Headers.ContentType.CharSet = "utf-8";
            HttpResponseMessage result = client.PostAsync(MsStoreUri.cookieUri, stringContent).Result;
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception("GetMsStoreCookieError");
            }
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
        /// <summary>
        /// 获取CategoryID
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetProductCategoryID(HttpClient client)
        {
            var httpResponseMessage = client.GetStringAsync(MsStoreUri.productUri).Result;
            if (httpResponseMessage != null || httpResponseMessage != string.Empty)
            {
                var jsonNode = JsonObject.Parse(httpResponseMessage)["Payload"];
                JsonArray? payload = jsonNode["Skus"].AsArray();
                string? node = payload[0]["FulfillmentData"].ToString();
                var fullfillmentdata = JsonObject.Parse(node);
                
                return fullfillmentdata["WuCategoryId"].ToString();
            }
            else
            {
                throw new Exception("获取CategoryID错误");
            }

            return null;
        }

        public static string GetFileListXml(HttpClient client,string cookie,string CategoryID)
        {
            string data = DEFINE.WU_API.Replace("{1}", cookie).Replace("{2}", CategoryID).Replace("{3}", "ProductId");
            StringContent content = new (data);
            content.Headers.Expires = DateTime.Now;
            content.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml");
            content.Headers.ContentLength = data.Length;
            content.Headers.ContentType.CharSet = "utf-8";
            HttpResponseMessage message = client.PostAsync(MsStoreUri.fileListXmlUri,content).Result;
            if (!message.IsSuccessStatusCode)
            {
                throw new Exception("获取文件xml错误");
            }
            string realfile = message.Content.ReadAsStringAsync().Result.Replace("&lt;", "<").Replace("&gt;", ">");
            
            return realfile;
        }

        public static List<AppxVersion> GetAppx(HttpClient client,string filexml)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.LoadXml(filexml);
            List<AppxVersion> appxPackagesInfoDict = [];
            XmlNodeList fileList = xmldoc.GetElementsByTagName("SecuredFragment");
            int i = 0;
           foreach (XmlNode node in fileList)
           {
               var parentNodeParentNode = node.ParentNode.ParentNode;
               var value = parentNodeParentNode.FirstChild.Attributes["UpdateID"].Value;
               var packageMoniker = parentNodeParentNode.OwnerDocument.GetElementsByTagName("AppxMetadata")[i].Attributes["PackageMoniker"].Value;
               var id = parentNodeParentNode.ParentNode.FirstChild.FirstChild.Value;
               AppxVersion appxVersion = new()
               {
                   appxname = packageMoniker,
                   download_id = value,
                   id = id
               };
               appxPackagesInfoDict.Add(appxVersion);
               i++;
           }
           return appxPackagesInfoDict;
        }
        public static string GetPackageIdentityName(string packageFamilyName)
        {
            int lastUnderscoreIndex = packageFamilyName.LastIndexOf('_');
            return lastUnderscoreIndex >= 0
                ? packageFamilyName[..lastUnderscoreIndex]
                : packageFamilyName;
        }
        public static (string Version, string Arch)? GetPackageVersionAndArch(string packageMoniker)
        {
            int firstIdx = packageMoniker.IndexOf('_');
            int secondIdx = packageMoniker.IndexOf('_', firstIdx + 1);
            int thirdIdx = packageMoniker.IndexOf('_', secondIdx + 1);

            if (firstIdx != -1 && secondIdx != -1 && thirdIdx != -1)
            {
                string version = packageMoniker[(firstIdx + 1)..secondIdx];
                string arch = packageMoniker[(secondIdx + 1)..thirdIdx];
                return (version, arch);
            }

            return null;
        }
        public static (int, int, int, int, int) AppxVer2GameVer(string appxVersion)
        {
            string[] arr = appxVersion.Split('.');
            
            if (arr.Length >= 3)
            {
                int n = 4 - arr[2].Length;
                if (n > 0)
                {
                    arr[2] = new string('0', n) + arr[2];
                }
            }

            string major = arr[0];
            string minor = arr[1];
            string patch = arr[2].Substring(0, arr[2].Length - 2);
            string revision = arr[2].Substring(arr[2].Length - 2);
            string fifth = arr[3];

            return (
                int.Parse(major),
                int.Parse(minor),
                int.Parse(patch),
                int.Parse(revision),
                int.Parse(fifth)
            );
        }
        public static string GameVer2Str((int major, int minor, int patch, int revision, int fifth) gameVer,
            bool withFifth = false)
        {
            return withFifth
                ? $"{gameVer.major}.{gameVer.minor}.{gameVer.patch}.{gameVer.revision}.{gameVer.fifth}"
                : $"{gameVer.major}.{gameVer.minor}.{gameVer.patch}.{gameVer.revision}";
        }
        public static void GetVersion(List<AppxVersion> version)
        {
           
        }

    }
}
