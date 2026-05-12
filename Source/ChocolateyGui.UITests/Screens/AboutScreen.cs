using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace ChocolateyGui.UITests.Screens
{
    public class AboutScreen : Window
    {
        public AboutScreen(FrameworkAutomationElementBase frameworkAutomationElement)
            : base(frameworkAutomationElement)
        {
        }

        public Button BackButton => this.Parent.FindFirstDescendant(cf => cf.ByAutomationId(AutomationIds.BACK_BUTTON)).AsButton();
    }
}