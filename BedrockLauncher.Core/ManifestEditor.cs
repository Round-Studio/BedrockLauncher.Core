using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BedrockLauncher.Core
{
    public static class ManifestEditor
    {
        private const string SCCD_BASE64 = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPEN1c3RvbUNhcGFiaWxpdHlEZXNjcmlwdG9yIHhtbG5zPSJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL2FwcHgvMjAxOC9zY2NkIiB4bWxuczpzPSJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL2FwcHgvMjAxOC9zY2NkIj4KICA8Q3VzdG9tQ2FwYWJpbGl0aWVzPgogICAgPEN1c3RvbUNhcGFiaWxpdHkgTmFtZT0iTWljcm9zb2Z0LmNvcmVBcHBBY3RpdmF0aW9uXzh3ZWt5YjNkOGJid2UiPjwvQ3VzdG9tQ2FwYWJpbGl0eT4KICA8L0N1c3RvbUNhcGFiaWxpdGllcz4KICA8QXV0aG9yaXplZEVudGl0aWVzIEFsbG93QW55PSJ0cnVlIi8+CiAgPENhdGFsb2c+RkZGRjwvQ2F0YWxvZz4KPC9DdXN0b21DYXBhYmlsaXR5RGVzY3JpcHRvcj4=";

        /// <summary>
        /// Modifies the AppxManifest.xml file and adds a CustomCapability.SCCD file 
        /// </summary>
        /// <returns>True if the operation succeeded; otherwise, false.</returns>
        public static bool EditManifest(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));

            string manifestPath = Path.Combine(directory, "AppxManifest.xml");
            if (!File.Exists(manifestPath))
                return false;
            try
            {
                //Load
                XDocument doc = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
                XNamespace ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
                XNamespace rescap = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";
                XNamespace uap4 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/4";
                XNamespace uap10 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/10";

                XElement package = doc.Root;
                if (package == null)
                    return false;

                UpdateIgnorableNamespaces(package, ns, rescap, uap4, uap10);
                UpdateApplicationTrustLevel(package, ns, uap10);
                UpdateCapabilities(package, ns, rescap, uap4);

                doc.Save(manifestPath, SaveOptions.DisableFormatting);

                string sccdPath = Path.Combine(directory, "CustomCapability.SCCD");
                File.WriteAllBytes(sccdPath, Convert.FromBase64String(SCCD_BASE64));

                return true;
            }
            catch
            {
                throw;
            }
        }

        private static void UpdateIgnorableNamespaces(XElement package, XNamespace ns, XNamespace rescap, XNamespace uap4, XNamespace uap10)
        {
            XAttribute ignorable = package.Attribute("IgnorableNamespaces");
            string[] requiredNamespaces = { "uap", "uap4", "uap10", "rescap" };

            if (ignorable == null)
            {
                package.SetAttributeValue("IgnorableNamespaces", string.Join(" ", requiredNamespaces));
            }
            else
            {
                var existingNamespaces = ignorable.Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var missingNamespaces = requiredNamespaces.Except(existingNamespaces);
                if (missingNamespaces.Any())
                {
                    ignorable.Value = string.Join(" ", existingNamespaces.Concat(missingNamespaces));
                }
            }

            package.SetAttributeValue(XNamespace.Xmlns + "rescap", rescap.NamespaceName);
            package.SetAttributeValue(XNamespace.Xmlns + "uap4", uap4.NamespaceName);
            package.SetAttributeValue(XNamespace.Xmlns + "uap10", uap10.NamespaceName);
        }

        private static void UpdateApplicationTrustLevel(XElement package, XNamespace ns, XNamespace uap10)
        {
            XElement applications = package.Element(ns + "Applications");
            XElement application = applications?.Element(ns + "Application");
            application?.SetAttributeValue(uap10 + "TrustLevel", "mediumIL");
        }

        private static void UpdateCapabilities(XElement package, XNamespace ns, XNamespace rescap, XNamespace uap4)
        {
            XElement capabilities = package.Element(ns + "Capabilities");
            if (capabilities == null)
                return;

            capabilities.Elements(rescap + "Capability").Remove();
            capabilities.Elements(uap4 + "CustomCapability").Remove();
            var deviceCapabilities = capabilities.Elements(ns + "DeviceCapability").ToList();
            deviceCapabilities.ForEach(c => c.Remove());

            capabilities.Add(
                new XElement(rescap + "Capability", new XAttribute("Name", "runFullTrust")),
                new XElement(uap4 + "CustomCapability", new XAttribute("Name", "Microsoft.coreAppActivation_8wekyb3d8bbwe"))
            );


            if (deviceCapabilities.Count > 0)
            {
                deviceCapabilities.ForEach(capabilities.Add);
            }
            else
            {
                capabilities.Add(new XElement(ns + "DeviceCapability", new XAttribute("Name", "internetClient")));
            }
        }
    }
}
