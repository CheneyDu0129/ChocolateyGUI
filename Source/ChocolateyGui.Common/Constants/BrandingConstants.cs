// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="BrandingConstants.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;

namespace ChocolateyGui.Common.Constants
{
    public static class BrandingConstants
    {
        public const string CompanyDirectoryName = "Semight Instruments";
        public const string ProductDirectoryName = "PackageManager";

        public static readonly string ProductPathName = Path.Combine(CompanyDirectoryName, ProductDirectoryName);
    }
}