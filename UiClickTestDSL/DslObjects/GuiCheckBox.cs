using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiCheckBox {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var res = window.FindAllChildrenByControlType(ControlType.CheckBox);
            return res;
        }

        public static IEnumerable<GuiCheckBox> GetAllCheckBoxes(AutomationElement window) {
            var res = window.FindAllChildrenByControlType(ControlType.CheckBox);
            return res.Select(cb => new GuiCheckBox(cb));
        }

        public static GuiCheckBox Find(AutomationElement window, string caption) {
            var res = window.FindChildByControlTypeAndName(ControlType.CheckBox, caption);
            return new GuiCheckBox(res);
        }

        internal static GuiCheckBox Find(AutomationElement window, ByAutomationId automationId) {
            var res = window.FindChildByControlTypeAndAutomationId(ControlType.CheckBox, automationId.Value);
            return new GuiCheckBox(res);
        }


        private readonly AutomationElement _cb;
        private readonly TogglePattern _toggler;
        public GuiCheckBox(AutomationElement el) {
            _cb = el;
            _toggler = _cb.GetPattern<TogglePattern>(TogglePattern.Pattern);
        }

        public string Caption { get { return _cb.Current.Name; } }

        public void ShouldBeDisabled() {
            Assert.IsFalse(_cb.Current.IsEnabled, _cb.Current.Name + " was not disabled.");
        }

        public void ShouldBeEnabled() {
            Assert.IsTrue(_cb.Current.IsEnabled, _cb.Current.Name + " was disabled.");
        }
        public bool IsChecked { get { return _toggler.Current.ToggleState == ToggleState.On; } }
        public bool IsUnChecked { get { return _toggler.Current.ToggleState == ToggleState.Off; } }
        public bool IsIndeterminate { get { return _toggler.Current.ToggleState == ToggleState.Indeterminate; } }

        public void ShouldBeChecked() { Assert.IsTrue(IsChecked); }
        public void ShouldNotBeChecked() { Assert.IsTrue(IsUnChecked); }

        public void Check() {
            while (!IsChecked) {
                _toggler.Toggle();
            }
        }

        public void UnCheck() {
            while (!IsUnChecked) {
                _toggler.Toggle();
            }
        }
    }
}
