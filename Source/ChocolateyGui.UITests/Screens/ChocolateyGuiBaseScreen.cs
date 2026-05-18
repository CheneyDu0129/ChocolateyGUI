using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace ChocolateyGui.UITests.Screens
{
    public class ChocolateyGuiBaseScreen : Window
    {
        public ChocolateyGuiBaseScreen(FrameworkAutomationElementBase frameworkAutomationElement)
            : base(frameworkAutomationElement)
        {
        }

        protected AutomationElement FindItemByTextBlockName(AutomationElement list, string wantedName)
        {
            if (list == null || string.IsNullOrWhiteSpace(wantedName))
            {
                return null;
            }

            var items = list.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem));

            return items.FirstOrDefault(item =>
                item.FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.Text)
                      .And(cf.ByName(wantedName))
                ) != null);
        }
    }
}
