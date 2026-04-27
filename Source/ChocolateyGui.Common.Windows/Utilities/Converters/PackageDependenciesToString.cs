// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="PackageDependenciesToString.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ChocolateyGui.Common.Windows.Utilities.Converters
{
    public class PackageDependenciesToString : IValueConverter
    {
        private static readonly Regex PackageNameVersionRegex = new Regex(@"^(?<Id>[A-Za-z0-9._-]+)(:{1,2})(?<Version>.+)$");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dependenciesString = value as string;
            if (string.IsNullOrWhiteSpace(dependenciesString))
            {
                return string.Empty;
            }

            var dependencyStrings = dependenciesString.Split('|');
            var items = dependencyStrings
                .Select(dependency =>
                {
                    var token = dependency?.Trim();
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        return string.Empty;
                    }

                    var match = PackageNameVersionRegex.Match(token);
                    if (!match.Success)
                    {
                        return ToDisplayName(token);
                    }

                    var id = match.Groups["Id"]?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        return token;
                    }

                    var displayName = ToDisplayName(id);
                    var version = match.Groups["Version"]?.Value?.Trim();
                    return string.IsNullOrWhiteSpace(version)
                        ? displayName
                        : displayName + " (" + version + ")";
                })
                .Where(dependency => !string.IsNullOrEmpty(dependency));

            return string.Join(", ", items);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string ToDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return string.Empty;
            }

            var normalized = id.Replace('.', ' ').Replace('-', ' ').Replace('_', ' ');
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized.ToLowerInvariant());
        }
    }
}