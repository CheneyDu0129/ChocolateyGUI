using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace ChocolateyGui.UITests.Screens
{
    public class PackageDetailsScreen : Window
    {
        public PackageDetailsScreen(FrameworkAutomationElementBase frameworkAutomationElement)
            : base(frameworkAutomationElement)
        {
        }

        public Button BackButton => this.Parent.FindFirstDescendant(cf => cf.ByAutomationId("Back")).AsButton();
    }
}