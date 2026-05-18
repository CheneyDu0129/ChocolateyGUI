using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using System;
using System.Linq;
using System.Threading;

namespace ChocolateyGui.UITests.Screens
{
    public class MainScreen : ChocolateyGuiBaseScreen
    {
        public MainScreen(FrameworkAutomationElementBase frameworkAutomationElement)
            : base(frameworkAutomationElement)
        {
        }

        public AboutScreen OpenAndGetAboutScreen()
        {
            var aboutButton = Retry.Find(
                () =>
                {
                    var button = FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.SHOW_ABOUT_BUTTON));
                    return button != null && button.IsEnabled ? button : null;
                },
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find enabled about button"
                });

            if (aboutButton.Patterns.Invoke.IsSupported)
            {
                aboutButton.Patterns.Invoke.Pattern.Invoke();
            }
            else
            {
                aboutButton.Click();
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
                        TimeoutMessage = "Failed to find about screen"
                    })
                .As<AboutScreen>();
        }

        public SettingsScreen OpenAndGetSettingsScreen()
        {
            var settingsButton = Retry.Find(
                () =>
                {
                    var button = FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.SHOW_SETTINGS_BUTTON));
                    return button != null && button.IsEnabled ? button : null;
                },
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(20),
                    Interval = TimeSpan.FromMilliseconds(200),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find enabled settings button"
                });

            if (settingsButton.Patterns.Invoke.IsSupported)
            {
                settingsButton.Patterns.Invoke.Pattern.Invoke();
            }
            else
            {
                settingsButton.Click();
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
                        TimeoutMessage = "Failed to find settings screen"
                    })
                .As<SettingsScreen>();
        }

        public RemoteSourceScreen OpenAndGetRemoteSourceScreen(string sourceName = "chocolatey")
        {
            if (!string.IsNullOrWhiteSpace(sourceName))
            {
                var sourceComboBox = Retry.Find(
                        () => FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)),
                        new RetrySettings
                        {
                            Timeout = TimeSpan.FromSeconds(20),
                            Interval = TimeSpan.FromMilliseconds(200),
                            IgnoreException = true,
                            ThrowOnTimeout = true,
                            TimeoutMessage = "Failed to find source selector"
                        })
                    .AsComboBox();

                var candidateSources = string.Equals(sourceName, "hermes", StringComparison.OrdinalIgnoreCase)
                    ? new[] { sourceName, "ept-stable" }
                    : new[] { sourceName };

                var selectedText = sourceComboBox.SelectedItem?.Text;
                var isAlreadySelected = !string.IsNullOrWhiteSpace(selectedText)
                    && candidateSources.Any(candidate => selectedText.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!isAlreadySelected)
                {
                    sourceComboBox.Expand();

                    var sourceItem = Retry.Find(
                        () =>
                        {
                            var comboItems = sourceComboBox.Items;
                            var matchFromCombo = comboItems.FirstOrDefault(item => candidateSources.Any(candidate =>
                                (!string.IsNullOrWhiteSpace(item.Text) && item.Text.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                                || (!string.IsNullOrWhiteSpace(item.Name) && item.Name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)));

                            if (matchFromCombo != null)
                            {
                                return matchFromCombo;
                            }

                            var listItems = FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));
                            return listItems.FirstOrDefault(item => candidateSources.Any(candidate =>
                                (!string.IsNullOrWhiteSpace(item.Name) && item.Name.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)
                                || (!string.IsNullOrWhiteSpace(item.Properties.Name.ValueOrDefault) && item.Properties.Name.ValueOrDefault.IndexOf(candidate, StringComparison.OrdinalIgnoreCase) >= 0)));
                        },
                        new RetrySettings
                        {
                            Timeout = TimeSpan.FromSeconds(8),
                            Interval = TimeSpan.FromMilliseconds(200),
                            IgnoreException = true,
                            ThrowOnTimeout = false
                        });

                    if (sourceItem != null)
                    {
                        if (sourceItem.Patterns.SelectionItem.IsSupported)
                        {
                            sourceItem.Patterns.SelectionItem.Pattern.Select();
                        }
                        else if (sourceItem.Patterns.Invoke.IsSupported)
                        {
                            sourceItem.Patterns.Invoke.Pattern.Invoke();
                        }
                        else
                        {
                            sourceItem.Click();
                        }

                        WaitForDialog();
                    }
                }
            }

            Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.SEARCH_TEXT_BOX)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(45),
                    Interval = TimeSpan.FromMilliseconds(250),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find remote source search textbox"
                });

            Retry.Find(
                () => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.PACKAGES_LIST)),
                new RetrySettings
                {
                    Timeout = TimeSpan.FromSeconds(45),
                    Interval = TimeSpan.FromMilliseconds(250),
                    IgnoreException = true,
                    ThrowOnTimeout = true,
                    TimeoutMessage = "Failed to find remote source packages list"
                });

            return new RemoteSourceScreen(FrameworkAutomationElement);
        }

        public string GetCurrentSourceText()
        {
            var sourceComboBox = Retry.Find(
                    () => FindFirstDescendant(cf => cf.ByControlType(ControlType.ComboBox)),
                    new RetrySettings
                    {
                        Timeout = TimeSpan.FromSeconds(10),
                        Interval = TimeSpan.FromMilliseconds(200),
                        IgnoreException = true,
                        ThrowOnTimeout = false
                    })
                ?.AsComboBox();

            return sourceComboBox?.SelectedItem?.Text;
        }

        public void WaitForDialog()
        {
            // The dialog has a fade in and out, sometimes this messes with the detection of the dialog.
            // As such, we sleep for half a second on each side of the detection.
            Thread.Sleep(500);

            Retry.WhileNotNull(() => FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.DIALOG)),
                timeout: TimeSpan.FromSeconds(120),
                interval: TimeSpan.FromMilliseconds(100),
                throwOnTimeout: true);

            Thread.Sleep(500);
        }
    }
}