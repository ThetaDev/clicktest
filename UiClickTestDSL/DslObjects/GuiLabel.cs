using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using System;
using UiClickTestDSL.HelperPrograms;

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

        public static GuiLabel GetLabelByName(AutomationElement window, string name) {
            var res = window.FindChildByControlTypeAndName(ControlType.Text, name);
            return new GuiLabel(res);
        }

        public AutomationElement LabelElement;

        public GuiLabel(AutomationElement label) {
            LabelElement = label;
        }

        public string Text { get { return LabelElement.Current.Name; } }

        public bool Visible { get { return !LabelElement.Current.IsOffscreen; } }

        public void ShouldRead(string text, string additionalInformation = "") {
            Assert.AreEqual(text, Text, "Label text is wrong. " + additionalInformation);
        }

        public string DisplayText {
            get {
                var selectionPattern = LabelElement.GetPattern<SelectionPattern>(SelectionPattern.Pattern);
                var selection = selectionPattern.Current.GetSelection();
                var first = selection.First();
                var  Item = new GuiLabel(first);
                return Item.Text;
            }
        }

        public void ShouldReadContaining(string text) {
            string displayed = DisplayText;
            Assert.IsTrue(displayed.ContainsIgnoreCase(text), "Wrong value in combobox, should contain: " + text + ", was: " + displayed);
        }

        public void ShouldNotReadContaining(string text) {
            string displayed = DisplayText;
            Assert.AreNotEqual(text, displayed.ContainsIgnoreCase(text));
            //Assert.IsFalse(displayed.ContainsIgnoreCase(text), "Wrong value in combobox, should not contain: " + text + ", was: " + displayed);
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

        /*
        public void ShouldNotBeVisible() {
            Assert.IsFalse(Visible);
        }*/

        public void ShouldBeSetToDaysFromNowDate(int days) {
            var valueday = (DateTime.Now + TimeSpan.FromDays(days)).Day.ToString();
            var valuemonth = (DateTime.Now + TimeSpan.FromDays(days)).Month.ToString();
            var valueyear = (DateTime.Now + TimeSpan.FromDays(days)).Year.ToString();
            ShouldReadContaining(valueday + "." + valuemonth + "." + valueyear);
        }

        public void ShouldBeSetToDaysFromNow(int days) {
            ShouldRead((DateTime.Now + TimeSpan.FromDays(days)).Date.ToShortDateString());      }
    }
}
