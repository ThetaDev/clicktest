using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using UiClickTestDSL.HelperPrograms;

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

        public static GuiTextBox GetTextBoxByName(AutomationElement window, string name) {
            if (_cachedtb == null || _cachedtb.Name != name) {
                var tb = window.FindChildByControlTypeAndName(ControlType.Edit, name);
                _cachedtb = new GuiTextBox(tb);
            }
            return _cachedtb;
        }

        public static void InvalidateCache() {
            _cachedtb = null;
        }

        protected AutomationElement tbAutoEl;
        protected ValuePattern value;
        public string AutomationId { get; protected set; }
        public string Name { get; protected set; }

        public GuiTextBox(AutomationElement textbox) {
            tbAutoEl = textbox;
            AutomationId = textbox.Current.AutomationId;
            Name = textbox.Current.Name;
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

        public void Type(object text) {
            Type(text.ToString());
        }

        public void Type(string text) {
            //Focus();
            var completeText = Text + text;
            Workarounds.TryUntilElementAvailable(() => value.SetValue(completeText));
        }

        public void SetText(object text) {
            SetText(text.ToString());
        }

        public void SetText(string text) {
            //Focus();
            Workarounds.TryUntilElementAvailable(() => value.SetValue(text));
        }

        public void Focus() {
            Workarounds.TryUntilElementAvailable(() => tbAutoEl.SetFocus());
        }

        public void ShouldRead(object expected) {
            ShouldRead(expected.ToString());
        }

        public void ShouldRead(string expected) {
            Assert.AreEqual(expected, Text);
        }
        public void ShouldNotRead(object expected) {
            ShouldNotRead(expected.ToString());
        }
        public void ShouldNotRead(string expected) {
            Assert.AreNotEqual(expected, Text);
        }
        public void ShouldStartWith(string startText) {
            Assert.IsTrue(Text.StartsWith(startText, StringComparison.CurrentCultureIgnoreCase), $"Expected TextBox to start with <{startText}>. Actual: <{Text}>.");
        }
        public void ShouldNotStartWith(string startText) {
            Assert.IsFalse(Text.StartsWith(startText, StringComparison.CurrentCultureIgnoreCase), $"Expected TextBox to not start with <{startText}>. Actual: <{Text}>.");
        }

        public void ShouldReadLines(params string[] expectedLines) {
            var expected = new StringBuilder();
            foreach (var line in expectedLines) {
                expected.AppendLine(line);
            }
            expected.Length -= Environment.NewLine.Length;
            ShouldRead(expected.ToString());
        }

        public void ShouldContain(params string[] expectedTexts) {
            foreach (var expectedText in expectedTexts) {
                Assert.IsTrue(Text.ContainsIgnoreCase(expectedText), $"TextBox did not contain <{expectedText}>. Actual: <{Text}>.");
            }
        }

        public void AssertIsEditable() {
            Assert.IsTrue(IsEditable);
        }

        public void AssertIsNotEditable() {
            Assert.IsFalse(IsEditable);
        }

        /*
        public void ShouldNotBeVisible() {
            Assert.IsTrue(tbAutoEl.Current.IsOffscreen, $"TextBox: {Name} ({AutomationId}) should have been offscreen");
        }*/

        public void ShouldBeVisible() {
            Assert.IsFalse(tbAutoEl.Current.IsOffscreen, $"TextBox: {Name} ({AutomationId}) should have been visible");
        }
    }
}
