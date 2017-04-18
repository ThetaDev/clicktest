using System.Collections.Generic;
using System.Windows.Automation;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiUserControl : UiTestDslCoreCommon {
        private static string _automationId;
        private static AutomationElement _currentParentWindow;

        public GuiUserControl(AutomationElement element) {
            Window = element;
        }

        public static GuiUserControl GetUserControl(AutomationElement window, string name) {
            _automationId = name;
            _currentParentWindow = window;
            AutomationElement ele;
            ele = window.FindChildByControlTypeAndAutomationId(ControlType.Custom, name);
            return new GuiUserControl(ele);
        }

        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var children = window.FindAllChildrenByControlType(ControlType.Custom);
            return children;
        }

        public override void GetThisWindow() {
            Window = GetUserControl(_currentParentWindow, _automationId).Window;
        }
    }
}