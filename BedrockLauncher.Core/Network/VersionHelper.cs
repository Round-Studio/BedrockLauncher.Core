using BedrockLauncher.Core.JsonHandle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
namespace BedrockLauncher.Core.Network
{
    public struct MsStoreUri
    {
        public static Uri cookieUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        public static Uri fileListXmlUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx");
        public static Uri updateUri = new("https://fe3.delivery.mp.microsoft.com/ClientWebService/client.asmx/secured");
        public static Uri productUri = new("https://storeedgefd.dsx.mp.microsoft.com/v9.0/products/9NBLGGH2JHXJ?market=US&locale=en-US&deviceFamily=Windows.Desktop");
    }

    public static class VersionHelper
    {
        public static string GetUri(HttpClient client,string update_id)
        {
            DateTime now = DateTime.UtcNow;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(DEFINE.FE3FileUrl);
            var elementsByTagName = xmlDoc.GetElementsByTagName("UpdateID");
            elementsByTagName[0].InnerText = update_id;
            var create = xmlDoc.GetElementsByTagName("Created");
            create[0].InnerText = now.ToString("o");
            var expires = xmlDoc.GetElementsByTagName("Expires");
            expires[0].InnerText = now.AddMinutes(5).ToString("o");
            var deviceAttributes = xmlDoc.GetElementsByTagName("deviceAttributes");
            deviceAttributes[0].InnerText =
                "E:BranchReadinessLevel=CBB&amp;DchuNvidiaGrfxExists=1&amp;ProcessorIdentifier=Intel64%20Family%206%20Model%2063%20Stepping%202&amp;CurrentBranch=rs4_release&amp;DataVer_RS5=1942&amp;FlightRing=Retail&amp;AttrDataVer=57&amp;InstallLanguage=en-US&amp;DchuAmdGrfxExists=1&amp;OSUILocale=en-US&amp;InstallationType=Client&amp;FlightingBranchName=&amp;Version_RS5=10&amp;UpgEx_RS5=Green&amp;GStatus_RS5=2&amp;OSSkuId=48&amp;App=WU&amp;InstallDate=1529700913&amp;ProcessorManufacturer=GenuineIntel&amp;AppVer=10.0.17134.471&amp;OSArchitecture=AMD64&amp;UpdateManagementGroup=2&amp;IsDeviceRetailDemo=0&amp;HidOverGattReg=C%3A%5CWINDOWS%5CSystem32%5CDriverStore%5CFileRepository%5Chidbthle.inf_amd64_467f181075371c89%5CMicrosoft.Bluetooth.Profiles.HidOverGatt.dll&amp;IsFlightingEnabled=0&amp;DchuIntelGrfxExists=1&amp;TelemetryLevel=1&amp;DefaultUserRegion=244&amp;DeferFeatureUpdatePeriodInDays=365&amp;Bios=Unknown&amp;WuClientVer=10.0.17134.471&amp;PausedFeatureStatus=1&amp;Steam=URL%3Asteam%20protocol&amp;Free=8to16&amp;OSVersion=10.0.17134.472&amp;DeviceFamily=Windows.Desktop";
            var str = xmlDoc.InnerXml;
            StringContent stxContent = new StringContent(str);
            stxContent.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml");
            stxContent.Headers.ContentType.CharSet = "utf-8";
            var postAsync = client.PostAsync(MsStoreUri.updateUri,stxContent).Result;
            if (postAsync.IsSuccessStatusCode)
            {
                var identifyComplexUrl = IdentifyComplexUrl(postAsync.Content.ReadAsStringAsync().Result);
               return identifyComplexUrl;
            }

            return null;
        }
        private static string IdentifyComplexUrl(string soapResponse)
        {
            try
            {
                XDocument xdoc = XDocument.Parse(soapResponse);
                XNamespace ns = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService";

                var urls = xdoc.Descendants(ns + "Url")
                    .Select(x => WebUtility.HtmlDecode(x.Value))
                    .ToList();

                foreach (var url in urls)
                {
                    if (url.Contains("?P1=") ||
                        url.Contains("tlu.dl.") ||
                        url.Contains("&P2=") ||
                        url.Contains("%3d") ||  // 编码后的等号
                        url.Length > 150)      // 长度明显更长
                    {
                        return url;
                    }
                }

                // 如果没有明显特征返回最后一个URL
                return urls.LastOrDefault();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static List<VersionInformation> GetVersions(HttpClient client,string uri)
        {
            
            var result = client.GetStringAsync(uri).Result;
            List<VersionInformation> versions = new List<VersionInformation>();
            var jsonNode = JsonObject.Parse(result).AsObject();
            foreach (var value in jsonNode)
            {
                var jsonArray = value.Value.AsObject();
                foreach (var i in jsonArray)
                {
                    var versionInformation = JsonSerializer.Deserialize<VersionInformation>(i.Value.ToJsonString());
                    versions.Add(versionInformation);
                    Console.WriteLine(versionInformation.ID);
                }
            }
            return versions;
        }
    }
}
