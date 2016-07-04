using System.Drawing;
using System.Windows.Automation;
using Microsoft.Test.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiCell {
        private readonly AutomationElement cell;
        private readonly string _owningColumnName;
        private readonly ValuePattern _value;

        public GuiCell(AutomationElement autoEl, string columnName) {
            cell = autoEl;
            _owningColumnName = columnName;
            _value = cell.GetPattern<ValuePattern>(ValuePattern.Pattern);
        }

        public string Text {
            get {
                return _value.Current.Value;
            }
        }

        public string ColumnHeader {
            get { return _owningColumnName; }
        }

        /* todo:
        public bool Editable {
            get {return cell.}
        }
         * */

        public void SetText(string text) {
            cell.SetFocus();
            _value.SetValue(text);
        }

        public void SetFocus() {
            cell.SetFocus();
        }

        public void Type(string text) {
            SetFocus();
            var completeText = Text + text;
            _value.SetValue(completeText);
        }

        public void DoubleClick() {
            var p = cell.GetClickablePoint();
            Mouse.MoveTo(new Point((int) p.X, (int) p.Y));
            Mouse.DoubleClick(MouseButton.Left);
        }

        public void ShouldRead(string expectedText) {
            Assert.AreEqual(expectedText.ToLower(), Text.ToLower(), "Cell was in column " + _owningColumnName);
        }

        public void ShouldNotRead(string expectedText) {
            Assert.AreNotEqual(expectedText.ToLower(), Text.ToLower(), "Cell was in column " + _owningColumnName);
        }

        public void RightClick() {
            cell.ClickPointInCenter(MouseButton.Right);
        }

        public void LeftClick() {
            cell.ClickPointInCenter(MouseButton.Left);
        }

        public GuiButton Button(ByAutomationId automationId) { return GuiButton.GetButtonByAutomationId(cell, automationId.Value); }
        public GuiButton Button(string caption) { return GuiButton.GetButton(cell, caption); }
        public GuiRadioButton RadioButton(string caption) { return GuiRadioButton.GetRadioButton(cell, caption); }
        public GuiCheckBox CheckBox(string caption) { return GuiCheckBox.Find(cell, caption); }
        public GuiCheckBox CheckBox(ByAutomationId automationId) { return GuiCheckBox.Find(cell, automationId); }
        public GuiComboBox ComboBox(string automationId) { return GuiComboBox.Find(cell, automationId); }
        public GuiComboBox ComboBox(ByAutomationId automationId) { return GuiComboBox.Find(cell, automationId.Value); }
    }
}