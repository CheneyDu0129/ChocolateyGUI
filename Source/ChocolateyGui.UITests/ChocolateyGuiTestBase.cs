using ChocolateyGui.TestUtilities;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA2;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace ChocolateyGui.UITests
{
    public class ChocolateyGuiTestBase : FlaUITestBase
    {
        protected override VideoRecordingMode VideoRecordingMode => VideoRecordingMode.NoVideo;

        protected override AutomationBase GetAutomation()
        {
            var automation = new UIA2Automation();
            return automation;
        }

        protected override Application StartApplication()
        {
            var testDirectory = TestContext.CurrentContext.TestDirectory.Replace(".UITests", "");
            var executablePath = Path.Combine(testDirectory, "ChocolateyGui.exe");
            var workingDirectory = Path.GetDirectoryName(executablePath);

            var testProfileRoot = Path.Combine(Path.GetTempPath(), "ChocolateyGui.UITests", TestContext.CurrentContext.WorkerId ?? "default");
            var localAppDataPath = Path.Combine(testProfileRoot, "LocalAppData");
            var chocolateyGuiLocalAppDataPath = Path.Combine(localAppDataPath, "Semight Instruments", "PackageManager");
            Directory.CreateDirectory(localAppDataPath);
            Directory.CreateDirectory(chocolateyGuiLocalAppDataPath);

            var processStartInfo = new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false
            };

            processStartInfo.EnvironmentVariables["LOCALAPPDATA"] = localAppDataPath;
            processStartInfo.EnvironmentVariables["ChocolateyGuiLocalAppDataPath"] = chocolateyGuiLocalAppDataPath;

            var application = Application.Launch(processStartInfo);

            var window = Retry.Find(
                () => application.GetMainWindow(Automation).FindFirstDescendant(cf => cf.ByAutomationId("PART_TitleBar")),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(45),
                    Interval = TimeSpan.FromMilliseconds(500),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Could not start the application."
                });

            window.WaitUntilClickable();
            return application;
        }
    }
}
