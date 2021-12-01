using System.Collections.Generic;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiRadioButton {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            IEnumerable<AutomationElement> tbs = window.FindAllChildrenByControlType(ControlType.RadioButton);
            return tbs;
        }

        public static GuiRadioButton GetRadioButtonByAutomationId(AutomationElement window, string automationId) {
            AutomationElement res = window.FindChildByControlTypeAndAutomationId(ControlType.RadioButton, automationId);
            return new GuiRadioButton(res);
        }

        public static GuiRadioButton GetRadioButton(AutomationElement window, string name) {
            AutomationElement res = window.FindChildByControlTypeAndName(ControlType.RadioButton, name);
            return new GuiRadioButton(res);
        }

        private AutomationElement btn;

        public GuiRadioButton(AutomationElement btnFound) {
            btn = btnFound;
        }

        public void Select() {
            SelectNoWait();
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        public void SelectNoWait() {
            var invoker = btn.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            invoker.Select();
        }

        public void ShouldBeDisabled() {
            Assert.IsFalse(btn.Current.IsEnabled, btn.Current.Name + " was not disabled.");
        }

        public void ShouldBeEnabled() {
            Assert.IsTrue(btn.Current.IsEnabled, btn.Current.Name + " was disabled.");
        }

        public void ShouldBeSelected(string additionalInfo = null) {
            var invoker = btn.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            Assert.IsTrue(invoker.Current.IsSelected, btn.Current.Name + " was not selected. " + (additionalInfo != null ? "At: " + additionalInfo : ""));
        }
        public void ShouldNotBeSelected(string additionalInfo = null) {
            var invoker = btn.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            Assert.IsFalse(invoker.Current.IsSelected, btn.Current.Name + " was selected. " + (additionalInfo != null ? "At: " + additionalInfo : ""));
        }
    }
}