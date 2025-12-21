using System;
using System.Collections.Generic;
using System.Text;
using Windows.Management.Deployment;

namespace BedrockLauncher.Core.Utils
{
	public static class CheckUwp
	{
		public static bool IsUwpPackageInstalled(string packageFamilyName)
		{
			if (string.IsNullOrWhiteSpace(packageFamilyName))
				throw new ArgumentException("PackageFamilyName can't be empty", nameof(packageFamilyName));

			try
			{
				var packageManager = new PackageManager();

				var packages = packageManager.FindPackagesForUser(string.Empty);

				foreach (var package in packages)
				{
					if (package.Id.FamilyName.Equals(packageFamilyName, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}

				return false;
			}
			catch
			{
				throw;
			}
		}
	}
}
