using ChocolateyGui.TestUtilities;
using ChocolateyGui.UITests.Screens;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using NUnit.Framework;
using System.Linq;

namespace ChocolateyGui.UITests
{
    [TestFixture]
    public class BetaPackagesTests : ChocolateyGuiTestBase
    {
        private const string PACKAGE_UNDER_TEST = "mixedpackage";
        private const string LATEST_STABLE_VERSION = "2.0.0";
        private const string LATEST_BETA_VERSION = "2.1.0-beta-1";
        protected new ApplicationStartMode ApplicationStartMode => ApplicationStartMode.OncePerFixture;

        private MainScreen MainScreen;
        private RemoteSourceScreen RemoteSourceScreen;

        [SetUp]
        public void Arrange()
        {
            MainScreen = Application.GetMainWindow(Automation).As<MainScreen>();

            RemoteSourceScreen = MainScreen.OpenAndGetRemoteSourceScreen("hermes");
            var currentSource = MainScreen.GetCurrentSourceText();
            TestContext.WriteLine($"[Arrange] Current source selected: {currentSource}");
            Assert.That(currentSource, Is.Not.Null.And.Matches<string>(s => s.ToLower().Contains("hermes") || s.ToLower().Contains("ept-stable")),
                $"当前源选择应为 hermes 或 ept-stable，但实际为：{currentSource}");
            RemoteSourceScreen.SetPrereleaseFilter(true);
            RemoteSourceScreen.SetAllVersionsFilter(false);
            RemoteSourceScreen.SearchForPackage(PACKAGE_UNDER_TEST);
        }

        [Test]
        public void RemoteScreenFindsStableVersionOfDesiredPackage()
        {
            var packageList = RemoteSourceScreen.GetPackageList();

            Assert.AreEqual(1, packageList.Length);

            var targetPackage = packageList.FirstOrDefault();
            targetPackage.Click();
            targetPackage.DoubleClick();
            var versionItem = MainScreen.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.VERSION_TEXT));

            Assert.That(string.Equals(LATEST_STABLE_VERSION, versionItem.Name));
        }

        [Test]
        public void RemoteScreenFindsPrereleaseVersionOfDesiredPackage()
        {
            RemoteSourceScreen.SetPrereleaseFilter(true);
            RemoteSourceScreen.SetAllVersionsFilter(false);
            RemoteSourceScreen.SearchForPackage(PACKAGE_UNDER_TEST);

            var packageList = RemoteSourceScreen.GetPackageList();

            Assert.AreEqual(1, packageList.Length);

            var targetPackage = packageList.FirstOrDefault();
            targetPackage.Click();
            targetPackage.DoubleClick();
            var versionItem = MainScreen.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.VERSION_TEXT));

            Assert.That(string.Equals(LATEST_BETA_VERSION, versionItem.Name));
        }

        [Test]
        public void RemoteScreenFindsAllStableVersionsOfDesiredPackage()
        {
            RemoteSourceScreen.SetPrereleaseFilter(false);
            RemoteSourceScreen.SetAllVersionsFilter(true);
            RemoteSourceScreen.SearchForPackage(PACKAGE_UNDER_TEST);

            var packageList = RemoteSourceScreen.GetPackageList();

            Assert.AreEqual(3, packageList.Length);

            var targetPackage = packageList.FirstOrDefault();
            targetPackage.Click();
            targetPackage.DoubleClick();
            var versionItem = MainScreen.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.VERSION_TEXT));

            Assert.That(string.Equals(LATEST_STABLE_VERSION, versionItem.Name));
        }

        [Test]
        public void RemoteScreenFindsAllVersionsOfDesiredPackage()
        {
            RemoteSourceScreen.SetPrereleaseFilter(true);
            RemoteSourceScreen.SetAllVersionsFilter(true);
            RemoteSourceScreen.SearchForPackage(PACKAGE_UNDER_TEST);

            var packageList = RemoteSourceScreen.GetPackageList();

            Assert.AreEqual(10, packageList.Length);

            var targetPackage = packageList.FirstOrDefault();
            targetPackage.Click();
            targetPackage.DoubleClick();
            var versionItem = MainScreen.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.VERSION_TEXT));

            Assert.That(string.Equals(LATEST_BETA_VERSION, versionItem.Name));
        }
    }
}