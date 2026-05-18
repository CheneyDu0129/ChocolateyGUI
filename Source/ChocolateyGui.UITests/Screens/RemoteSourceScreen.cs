using System;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;

namespace ChocolateyGui.UITests.Screens
{
    public class RemoteSourceScreen : ChocolateyGuiBaseScreen
    {
        public RemoteSourceScreen(FrameworkAutomationElementBase frameworkAutomationElement)
            : base(frameworkAutomationElement)
        {
        }

        public PackageDetailsScreen GetPackageDetailsScreen(string packageTitle)
        {
            var packagesListView = Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.PACKAGES_LIST)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find packages list"
                });

            var packageListItem = Retry.Find(
                () => FindItemByTextBlockName(packagesListView, packageTitle)
                      ?? packagesListView.FindFirstDescendant(cf => cf.ByControlType(ControlType.ListItem)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find package list item"
                });

            if (packageListItem.Patterns.SelectionItem.IsSupported)
            {
                packageListItem.Patterns.SelectionItem.Pattern.Select();
            }

            if (packageListItem.Patterns.Invoke.IsSupported)
            {
                packageListItem.Patterns.Invoke.Pattern.Invoke();
            }
            else
            {
                throw new NotSupportedException("Package list item does not support Invoke pattern and mouse input is not allowed in this environment.");
            }

            return Retry.Find(
                    () =>
                    {
                        var window = FindFirstChild(cf => cf.ByControlType(ControlType.Window));
                        return window != null && !ReferenceEquals(window.FrameworkAutomationElement, FrameworkAutomationElement) ? window : null;
                    },
                    new RetrySettings
                    {
                        Timeout = TimeSpan.FromSeconds(10),
                        Interval = TimeSpan.FromMilliseconds(200),
                        IgnoreException = true,
                        ThrowOnTimeout = true,
                        TimeoutMessage = "Failed to find package details screen"
                    })
                .As<PackageDetailsScreen>();
        }

        public void FocusAndClearSearch()
        {
            var search = Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.SEARCH_TEXT_BOX)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find search textbox"
                });

            if (search.Patterns.Value.IsSupported)
            {
                search.Patterns.Value.Pattern.SetValue(string.Empty);
            }
            else
            {
                search.AsTextBox().Text = string.Empty;
            }
        }

        public void SearchForPackage(string packageName)
        {
            var search = Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.SEARCH_TEXT_BOX)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find search textbox"
                });

            if (search.Patterns.Value.IsSupported)
            {
                search.Patterns.Value.Pattern.SetValue(packageName ?? string.Empty);
            }
            else
            {
                search.AsTextBox().Text = packageName ?? string.Empty;
            }

            WaitForDialog();
            WaitForAnyPackageResult();
        }

        public void SetPrereleaseFilter(bool enabled)
        {
            var checkbox = Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.PRERELEASE_CHECK_BOX)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find prerelease checkbox"
                });

            var togglePattern = checkbox.Patterns.Toggle.IsSupported ? checkbox.Patterns.Toggle.Pattern : null;
            var isChecked = togglePattern != null && togglePattern.ToggleState == FlaUI.Core.Definitions.ToggleState.On;

            if (isChecked != enabled)
            {
                if (togglePattern != null)
                {
                    togglePattern.Toggle();
                }
                else if (checkbox.Patterns.Invoke.IsSupported)
                {
                    checkbox.Patterns.Invoke.Pattern.Invoke();
                }
                else
                {
                    checkbox.Click();
                }

                WaitForDialog();
            }
        }

        public void SetAllVersionsFilter(bool enabled)
        {
            var checkbox = Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.ALL_VERSIONS_CHECK_BOX)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find all versions checkbox"
                });

            var togglePattern = checkbox.Patterns.Toggle.IsSupported ? checkbox.Patterns.Toggle.Pattern : null;
            var isChecked = togglePattern != null && togglePattern.ToggleState == FlaUI.Core.Definitions.ToggleState.On;

            if (isChecked != enabled)
            {
                if (togglePattern != null)
                {
                    togglePattern.Toggle();
                }
                else if (checkbox.Patterns.Invoke.IsSupported)
                {
                    checkbox.Patterns.Invoke.Pattern.Invoke();
                }
                else
                {
                    checkbox.Click();
                }

                WaitForDialog();
            }
        }

        public void WaitForAnyPackageResult()
        {
            Retry.Find(
                () =>
                {
                    var items = GetPackageList();
                    return items.Length > 0 ? items : null;
                },
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(30),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find package results"
                });
        }

        public AutomationElement[] GetPackageList()
        {
            var packagesList = Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.PACKAGES_LIST)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find packages list"
                });

            return packagesList.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
        }

        private void WaitForDialog()
        {
            Thread.Sleep(500);

            Retry.WhileNotNull(() => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.DIALOG)),
                timeout: TimeSpan.FromSeconds(120),
                interval: TimeSpan.FromMilliseconds(100),
                throwOnTimeout: true);

            Thread.Sleep(500);
        }
    }
}