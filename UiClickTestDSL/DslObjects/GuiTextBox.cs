using System.Collections.Generic;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiTextBox {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var tbs = window.FindAllChildrenByControlType(ControlType.Edit);
            return tbs;
        }

        private static GuiTextBox _cachedtb = null;
        public static GuiTextBox GetTextBox(AutomationElement window, string automationId) {
            if (_cachedtb == null || _cachedtb.AutomationId != automationId) {
                var tb = window.FindChildByControlTypeAndAutomationId(ControlType.Edit, automationId);
                _cachedtb = new GuiTextBox(tb);
            }
            return _cachedtb;
        }

        public static void InvalidateCache() {
            _cachedtb = null;
        }


        private AutomationElement tbAutoEl;
        private ValuePattern value;
        public string AutomationId { get; private set; }

        public GuiTextBox(AutomationElement textbox) {
            tbAutoEl = textbox;
            AutomationId = textbox.Current.AutomationId;
            value = tbAutoEl.GetPattern<ValuePattern>(ValuePattern.Pattern);
        }

        public string Text {
            get {
                return value.Current.Value;
            }
        }

        public bool IsEditable {
            get {
                return !value.Current.IsReadOnly && tbAutoEl.Current.IsKeyboardFocusable;
            }
        }

        public bool Visible {
            get {
                return !tbAutoEl.Current.IsOffscreen;
            }
        }

        public void Type(string text) {
            //Focus();
            Workarounds.TryUntilElementAvailable(() => value.SetValue(text));
        }

        public void Focus() {
            Workarounds.TryUntilElementAvailable(() => tbAutoEl.SetFocus());
        }

        public void ShouldRead(string expected) {
            Assert.AreEqual(expected, Text);
        }

        public void AssertIsEditable() {
            Assert.IsTrue(IsEditable);
        }

        public void AssertIsNotEditable() {
            Assert.IsFalse(IsEditable);
        }
    }
}
