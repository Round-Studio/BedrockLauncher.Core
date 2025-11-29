using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BedrockLauncher.Core.BackGround;

namespace BedrockLauncher.Core.Utils
{
	public static class ManifestEditor
	{
		private const string SCCD_BASE64 = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPEN1c3RvbUNhcGFiaWxpdHlEZXNjcmlwdG9yIHhtbG5zPSJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL2FwcHgvMjAxOC9zY2NkIiB4bWxuczpzPSJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL2FwcHgvMjAxOC9zY2NkIj4KICA8Q3VzdG9tQ2FwYWJpbGl0aWVzPgogICAgPEN1c3RvbUNhcGFiaWxpdHkgTmFtZT0iTWljcm9zb2Z0LmNvcmVBcHBBY3RpdmF0aW9uXzh3ZWt5YjNkOGJid2UiPjwvQ3VzdG9tQ2FwYWJpbGl0eT4KICA8L0N1c3RvbUNhcGFiaWxpdGllcz4KICA8QXV0aG9yaXplZEVudGl0aWVzIEFsbG93QW55PSJ0cnVlIi8+CiAgPENhdGFsb2c+RkZGRjwvQ2F0YWxvZz4KPC9DdXN0b21DYXBhYmlsaXR5RGVzY3JpcHRvcj4=";

		/// <summary>
		/// Modifies the AppxManifest.xml file and adds a CustomCapability.SCCD file
		/// </summary>
		/// <returns>True if the operation succeeded; otherwise, false.</returns>
		public async static Task<bool> EditManifest(string directory, string gameName, BackGroundConfig? editer)
		{
			if (string.IsNullOrEmpty(directory))
				throw new ArgumentNullException(nameof(directory));
			string manifestPath = Path.Combine(directory, "AppxManifest.xml");
			if (!File.Exists(manifestPath))
				return false;
			try
			{
			  return await Task.Run<bool>((() =>
				{
					//Load
					XDocument doc = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
					XNamespace ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
					XNamespace rescap = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";
					XNamespace uap4 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/4";
					XNamespace uap10 = "http://schemas.microsoft.com/appx/manifest/uap/windows10/10";
					XNamespace uap = "http://schemas.microsoft.com/appx/manifest/uap/windows10";
					XNamespace desktop4 = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/4";
					XElement package = doc.Root;
					if (package == null)
						return false;
					UpdateIgnorableNamespaces(package, ns, rescap, uap4, uap10);
					UpdateApplicationTrustLevel(package, ns, uap10);
					UpdateCapabilities(package, ns, rescap, uap4);
					XElement applications = package.Element(ns + "Applications");
					XElement application = applications?.Element(ns + "Application");
					XElement extenElement = application?.Element(ns + "Extensions");
					XElement identElement = package?.Element(ns + "Identity");
					identElement.SetAttributeValue("Version", TimeBasedVersion.GetVersion());
					extenElement.RemoveAll();
					application.SetAttributeValue(desktop4 + "SupportsMultipleInstances", "true");
					XElement? xElement = application.Element(uap + "VisualElements");
					if (!string.IsNullOrEmpty(gameName))
					{
						xElement.SetAttributeValue("DisplayName", gameName);
					}
					xElement.SetAttributeValue("AppListEntry", "none");
					if (editer.HasValue)
					{
						var element = xElement.Element(uap + "SplashScreen");
						if (!string.IsNullOrEmpty(editer.Value.FileFullPath))
						{
							var fileName = Path.GetFileName(editer.Value.FileFullPath);
							File.Copy(editer.Value.FileFullPath, Path.Combine(directory, fileName));
							element.SetAttributeValue("Image", fileName);
						}

						if (editer.Value.BackGroundColor.HasValue)
						{
							element.SetAttributeValue("BackgroundColor", editer.Value.BackGroundColor.Value.ToHex(true));
						}
					}
					doc.Save(manifestPath, SaveOptions.DisableFormatting);
					string sccdPath = Path.Combine(directory, "CustomCapability.SCCD");
					File.WriteAllBytes(sccdPath, Convert.FromBase64String(SCCD_BASE64));
					var readAllText = File.ReadAllText(manifestPath);
					var value = Regex.Match(readAllText, "<\\s*Extensions\\s*/\\s*>").Value;
					var replace = readAllText.Replace(value,
						" <Extensions>\r\n        <uap4:Extension Category=\"windows.loopbackAccessRules\">\r\n          <uap4:LoopbackAccessRules>\r\n            <uap4:Rule Direction=\"out\" PackageFamilyName=\"Microsoft.MEECC_8wekyb3d8bbwe\" />\r\n          </uap4:LoopbackAccessRules>\r\n        </uap4:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mcperf\">\r\n            <uap:DisplayName>MCPERF</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and import world</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCPERF</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mcshortcut\">\r\n            <uap:DisplayName>MCSHORTCUT</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and load world</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCSHORTCUT</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mcpack\">\r\n            <uap:DisplayName>MCPACK</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and import resource pack</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCPACK</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mcworld\">\r\n            <uap:DisplayName>MCWORLD</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and import world</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCWORLD</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mcproject\">\r\n            <uap:DisplayName>MCPROJECT</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and import project</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCPROJECT</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mceditoraddon\">\r\n            <uap:DisplayName>MCEDITORADDON</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and import editor addon</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCEDITORADDON</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.protocol\">\r\n          <uap:Protocol Name=\"ms-xbl-multiplayer\" />\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.protocol\">\r\n          <uap:Protocol Name=\"minecraft\" />\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mcaddon\">\r\n            <uap:DisplayName>MCADDON</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and import addon</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCADDON</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n        <uap:Extension Category=\"windows.fileTypeAssociation\" EntryPoint=\"App2\">\r\n          <uap:FileTypeAssociation Name=\"mctemplate\">\r\n            <uap:DisplayName>MCTEMPLATE</uap:DisplayName>\r\n            <uap:InfoTip>Launch Minecraft and import world template</uap:InfoTip>\r\n            <uap:SupportedFileTypes>\r\n              <uap:FileType>.MCTEMPLATE</uap:FileType>\r\n            </uap:SupportedFileTypes>\r\n          </uap:FileTypeAssociation>\r\n        </uap:Extension>\r\n      </Extensions>");
					File.WriteAllText(manifestPath, replace);
					return true;
				}));
			}
			catch
			{
				throw;
			}
		}
		private static void UpdateIgnorableNamespaces(XElement package, XNamespace ns, XNamespace rescap, XNamespace uap4, XNamespace uap10)
		{
			XNamespace desktop4 = "http://schemas.microsoft.com/appx/manifest/desktop/windows10/4";
			XAttribute ignorable = package.Attribute("IgnorableNamespaces");
			string[] requiredNamespaces = { "uap", "uap4", "uap10", "rescap", "desktop4" };

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
			package.SetAttributeValue(XNamespace.Xmlns + "desktop4", desktop4.NamespaceName);
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
