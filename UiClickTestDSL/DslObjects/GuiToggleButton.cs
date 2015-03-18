using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiToggleButton {
        public static GuiToggleButton GetButtonByAutomationId(AutomationElement parent, string automationId) {
            var res = parent.FindChildByControlTypeAndAutomationId(ControlType.Button, automationId);
            return new GuiToggleButton(res);
        }

        private readonly AutomationElement btn;
        private readonly TogglePattern toggler;

        public GuiToggleButton(AutomationElement btnFound) {
            btn = btnFound;
            toggler = btn.GetPattern<TogglePattern>(TogglePattern.Pattern);
        }

        public void Lock() {
            Check();
        }

        public void UnLock() {
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
