// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Chocolatey" file="ImageService.cs">
//   Copyright 2017 - Present Chocolatey Software, LLC
//   Copyright 2014 - 2017 Rob Reynolds, the maintainers of Chocolatey, and RealDimensions Software, LLC
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChocolateyGui.Common.Windows.Services
{
    public class ImageService : IImageService
    {
        public string SplashScreenImageName
        {
            get
            {
                // Always use the new splash image, fallback to chocolatey.png if missing
                return "semightlogo.png";
            }
        }

        public ImageSource PrimaryApplicationImage
        {
            get
            {
                var image = new BitmapImage(new Uri("pack://application:,,,/ChocolateyGui;component/chocolatey_logo.png", UriKind.RelativeOrAbsolute));
                image.Freeze();
                return image;
            }
        }

        public ImageSource SecondaryApplicationImage
        {
            get { return null; }
        }

        public Uri ToolbarIconUri
        {
            get { return new Uri("pack://application:,,,/chocolateyicon.ico"); }
        }
    }
}