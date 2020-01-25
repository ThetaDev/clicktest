using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiLabel {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window, string prefix = null) {
            var res = window.FindAllChildrenByControlType(ControlType.Text);
            if (prefix != null)
                res = res.Where(l => l.Current.AutomationId.StartsWith(prefix));
            return res;
        }

        public static GuiLabel GetLabel(AutomationElement window, string automationId) {
            var res = window.FindChildByControlTypeAndAutomationId(ControlType.Text, automationId);
            return new GuiLabel(res);
        }

        internal AutomationElement LabelElement;

        public GuiLabel(AutomationElement label) {
            LabelElement = label;
        }

        public string Text { get { return LabelElement.Current.Name; } }

        public bool Visible { get { return !LabelElement.Current.IsOffscreen; } }

        public void ShouldRead(string text, string additionalInformation = "") {
            Assert.AreEqual(text, Text, "Label text is wrong. " + additionalInformation);
        }

        public void ShouldNotRead(string text) {
            Assert.AreNotEqual(text, Text, "Label text should not be: " + text);
        }

        public Point GetScreenBottomRightPosition() {
            return LabelElement.Current.BoundingRectangle.BottomRight;
        }

        public void Focus() {
            LabelElement.SetFocus();
        }

        public override string ToString() {
            return LabelElement.Current.AutomationId + " (" + Text + ")";
        }

        public void ShouldBeVisible() {
            Assert.IsTrue(Visible);
        }

        public void ShouldNotBeVisible() {
            Assert.IsFalse(Visible);
        }
    }
}
