using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiMenuItem {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var tbs = window.FindAllChildrenByControlType(ControlType.MenuItem);
            return tbs;
        }

        public static GuiMenuItem GetMenuItemByAutomationId(AutomationElement window, string automationId) {
            var res = window.FindChildByControlTypeAndAutomationId(ControlType.MenuItem,  automationId);
            return new GuiMenuItem(res);
        }

        public static GuiMenuItem GetMenuItem(AutomationElement window, string name) {
            AutomationElement res;
            if (name == "Close")
                res = window.FindChildByControlTypeAndAutomationIdAndName(ControlType.MenuItem, "menuitem", name);
            else
                res = window.FindChildByControlTypeAndName(ControlType.MenuItem, name);
            return new GuiMenuItem(res);
        }

        public static GuiMenuItem GetFirstMenuItem(AutomationElement window) {
            AutomationElement automationElement = window.FindAllChildrenByControlType(ControlType.MenuItem).First();
            return new GuiMenuItem(automationElement);
        }

        private readonly AutomationElement _mnu;

        public GuiMenuItem(AutomationElement mnuFound) {
            _mnu = mnuFound;
        }

        public void Click() {
            ShouldBeEnabled();
            ClickNoWait();
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        public void ClickNoWait() {
            var invoker = _mnu.GetPattern<InvokePattern>(InvokePattern.Pattern);
            invoker.Invoke();
        }

        public void Check() {
            var toggler = _mnu.GetPattern<TogglePattern>(TogglePattern.Pattern);
            while (toggler.Current.ToggleState != ToggleState.On) {
                toggler.Toggle();
            }
        }

        public void UnCheck() {
            var toggler = _mnu.GetPattern<TogglePattern>(TogglePattern.Pattern);
            while (toggler.Current.ToggleState != ToggleState.Off) {
                toggler.Toggle();
            }
        }

        public void Expand() {
            ExpandNoWait();
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        private void ExpandNoWait() {
            var expander = _mnu.GetPattern<ExpandCollapsePattern>(ExpandCollapsePattern.Pattern);
            expander.Expand();
        }

        public void ShouldBeDisabled() {
            Assert.IsFalse(_mnu.Current.IsEnabled, _mnu.Current.Name + " was not disabled.");
        }

        public void ShouldBeEnabled() {
            Assert.IsTrue(_mnu.Current.IsEnabled, _mnu.Current.Name + " was disabled.");
        }
    }
}
