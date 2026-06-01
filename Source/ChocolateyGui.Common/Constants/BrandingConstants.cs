// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="BrandingConstants.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace ChocolateyGui.Common.Constants
{
    public static class BrandingConstants
    {
        private static readonly IDictionary<string, string> BrandingValues = LoadBrandingValues();

        public static readonly string CompanyDirectoryName = GetBrandingValue("CompanyDirectoryName", "Semight Instruments");
        public static readonly string ProductDirectoryName = GetBrandingValue("ProductDirectoryName", "PackageManager");
        public static readonly string PackageId = GetBrandingValue("PackageId", "instr-pkgmgr");
        public static readonly string AppMutexName = GetBrandingValue("AppMutexName", "Global\\instr-pkgmgr-mutex");
        public static readonly string CliMutexName = GetBrandingValue("CliMutexName", "Global\\instr-pkgmgr-cli-mutex");

        public static readonly string ProductPathName = Path.Combine(CompanyDirectoryName, ProductDirectoryName);

        private static string GetBrandingValue(string name, string defaultValue)
        {
            string value;
            return BrandingValues.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : defaultValue;
        }

        private static IDictionary<string, string> LoadBrandingValues()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "branding.config");

            if (!File.Exists(configPath))
            {
                return values;
            }

            foreach (var line in File.ReadAllLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separatorIndex).Trim();
                var value = line.Substring(separatorIndex + 1).Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    values[key] = value;
                }
            }

            return values;
        }
    }
}
