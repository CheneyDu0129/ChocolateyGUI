using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace ChocolateyGui.UITests.Screens
{
    public class SettingsScreen : Window
    {
        public SettingsScreen(FrameworkAutomationElementBase frameworkAutomationElement)
            : base(frameworkAutomationElement)
        {
        }

        public Button BackButton => this.Parent.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.BACK_BUTTON)).AsButton();
    }
}