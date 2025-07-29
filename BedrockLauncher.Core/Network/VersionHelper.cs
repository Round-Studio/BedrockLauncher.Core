
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
using Exception = System.Exception;
using Uri = System.Uri;
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
        private const string SoapContentType = "application/soap+xml; charset=utf-8";
        private static readonly string DeviceAttributes =
            "E:BranchReadinessLevel=CBB&amp;DchuNvidiaGrfxExists=1&amp;ProcessorIdentifier=Intel64%20Family%206%20Model%2063%20Stepping%202&amp;CurrentBranch=rs4_release&amp;DataVer_RS5=1942&amp;FlightRing=Retail&amp;AttrDataVer=57&amp;InstallLanguage=en-US&amp;DchuAmdGrfxExists=1&amp;OSUILocale=en-US&amp;InstallationType=Client&amp;FlightingBranchName=&amp;Version_RS5=10&amp;UpgEx_RS5=Green&amp;GStatus_RS5=2&amp;OSSkuId=48&amp;App=WU&amp;InstallDate=1529700913&amp;ProcessorManufacturer=GenuineIntel&amp;AppVer=10.0.17134.471&amp;OSArchitecture=AMD64&amp;UpdateManagementGroup=2&amp;IsDeviceRetailDemo=0&amp;HidOverGattReg=C%3A%5CWINDOWS%5CSystem32%5CDriverStore%5CFileRepository%5Chidbthle.inf_amd64_467f181075371c89%5CMicrosoft.Bluetooth.Profiles.HidOverGatt.dll&amp;IsFlightingEnabled=0&amp;DchuIntelGrfxExists=1&amp;TelemetryLevel=1&amp;DefaultUserRegion=244&amp;DeferFeatureUpdatePeriodInDays=365&amp;Bios=Unknown&amp;WuClientVer=10.0.17134.471&amp;PausedFeatureStatus=1&amp;Steam=URL%3Asteam%20protocol&amp;Free=8to16&amp;OSVersion=10.0.17134.472&amp;DeviceFamily=Windows.Desktop";

        public static string GetUri(string updateId)
        {
            try
            {
                string soapRequest = PrepareSoapRequest(updateId);

                string soapResponse = PostSoapRequest(MsStoreUri.updateUri.OriginalString, soapRequest);

                return IdentifyComplexUrl(soapResponse);
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"网络请求失败: {webEx.Message}");
                return null;
            }
            catch (XmlException xmlEx)
            {
                Console.WriteLine($"XML处理失败: {xmlEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
                return null;
            }
        }

        private static string PrepareSoapRequest(string updateId)
        {
            DateTime now = DateTime.UtcNow;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(DEFINE.FE3FileUrl);

            xmlDoc.GetElementsByTagName("UpdateID")[0].InnerText = updateId;
            xmlDoc.GetElementsByTagName("Created")[0].InnerText = now.ToString("o");
            xmlDoc.GetElementsByTagName("Expires")[0].InnerText = now.AddMinutes(5).ToString("o");
            xmlDoc.GetElementsByTagName("deviceAttributes")[0].InnerText = DeviceAttributes;

            return xmlDoc.InnerXml;
        }

        private static string PostSoapRequest(string url, string soapRequest)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = SoapContentType;

                byte[] responseBytes = client.UploadData(url, "POST", Encoding.UTF8.GetBytes(soapRequest));
                return Encoding.UTF8.GetString(responseBytes);
            }
        }

        private static string IdentifyComplexUrl(string soapResponse)
        {
            XDocument xdoc = XDocument.Parse(soapResponse);
            XNamespace ns = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService";

            var urls = xdoc.Descendants(ns + "Url")
                .Select(x => WebUtility.HtmlDecode(x.Value))
                .ToList();

            var complexUrl = urls.FirstOrDefault(url =>
                url.Contains("?P1=") ||
                url.Contains("tlu.dl.") ||
                url.Contains("&P2=") ||
                url.Contains("%3d") ||
                url.Length > 150);

            return complexUrl ?? urls.LastOrDefault();
        }

        public static List<VersionInformation> GetVersions(string uri)
        {
            
       
            using (WebClient client = new WebClient())
            {
                var result = client.DownloadString(uri);
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
}
