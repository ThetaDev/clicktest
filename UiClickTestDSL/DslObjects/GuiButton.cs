using System.Collections.Generic;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiButton {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var tbs = window.FindAllChildrenByControlType(ControlType.Button);
            return tbs;
        }

        public static GuiButton GetButtonByAutomationId(AutomationElement window, string automationId) {
            var res = window.FindChildByControlTypeAndAutomationId(ControlType.Button, automationId);
            return new GuiButton(res, automationId);
        }

        public static GuiButton GetButton(AutomationElement window, string name) {
            AutomationElement res;
            if (name == "Close")
                res = window.FindChildByControlTypeAndAutomationIdAndName(ControlType.Button, "button", name);
            else
                res = window.FindChildByControlTypeAndName(ControlType.Button, name);
            return new GuiButton(res, name);
        }

        private readonly AutomationElement _btn;
        private readonly string _name;

        public GuiButton(AutomationElement btnFound, string name) {
            _btn = btnFound;
            _name = name;
        }

        public void Click() {
            ClickNoWait();
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        public void ClickNoWait() {
            var invoker = _btn.GetPattern<InvokePattern>(InvokePattern.Pattern);
            invoker.Invoke();
        }

        public void ShouldBeDisabled() {
            Assert.IsFalse(_btn.Current.IsEnabled, _btn.Current.Name + " was not disabled.");
        }

        public void ShouldBeEnabled() {
            Assert.IsTrue(_btn.Current.IsEnabled, _btn.Current.Name + " was disabled.");
        }

        public static GuiButton GetAppCloseButton(AutomationElement window) {
            var searchConditions = new Condition[] {
                new PropertyCondition(AutomationElement.NameProperty, "Close"),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.IsContentElementProperty, false)
            };
            var el = AutomationExtensions.RunActualSearch(window, searchConditions);
            return new GuiButton(el, "AppCloseButton");
        }

        public void ShouldNotBeVisible() {
            Assert.IsTrue(_btn.Current.IsOffscreen, "Button: "+_name+" should have been offscreen");
        }
    }
}
