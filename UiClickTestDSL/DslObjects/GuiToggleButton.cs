using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiToggleButton {
        public static GuiToggleButton GetButtonByAutomationId(AutomationElement parent, string automationId) {
            var res = parent.FindChildByControlTypeAndAutomationId(ControlType.Button, automationId);
            return new GuiToggleButton(res);
        }
        public static GuiToggleButton GetButtonByName(AutomationElement parent, string name) {
            var res = parent.FindChildByControlTypeAndName(ControlType.Button,name);
            return new GuiToggleButton(res);
        }

        private readonly AutomationElement btn;
        private readonly TogglePattern toggler;

        public GuiToggleButton(AutomationElement btnFound) {
            btn = btnFound;
            toggler = btn.GetPattern<TogglePattern>(TogglePattern.Pattern);
        }

        public void Lock() {
            btn.SetFocus();
            Check();
        }

        public void UnLock() {
            btn.SetFocus();
            UnCheck();
        }

        public bool IsChecked { get { return toggler.Current.ToggleState == ToggleState.On; } }
        public bool IsUnChecked { get { return toggler.Current.ToggleState == ToggleState.Off; } }
        public bool IsIndeterminate { get { return toggler.Current.ToggleState == ToggleState.Indeterminate; } }

        public void ShouldBeChecked() { Assert.IsTrue(IsChecked); }
        public void ShouldNotBeChecked() { Assert.IsTrue(IsUnChecked); }
        public void ShouldBeDisabled() {
            Assert.IsFalse(btn.Current.IsEnabled, btn.Current.Name + " was not disabled.");
        }
        public void ShouldBeEnabled() {
            Assert.IsTrue(btn.Current.IsEnabled, btn.Current.Name + " was disabled.");
        }

        public void Check() {
            while (!IsChecked) {
                toggler.Toggle();
            }
        }

        public void UnCheck() {
            while (!IsUnChecked) {
                toggler.Toggle();
            }
        }
    }
}
