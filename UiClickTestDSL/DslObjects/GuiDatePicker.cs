using System;
using System.Linq;
using System.Windows.Automation;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiDatePicker : GuiUserControl {
        private static GuiDatePicker _cachedDp = null;
        public static GuiDatePicker GetDatePickerC(AutomationElement window, string automationId) {
            if (_cachedDp == null || _cachedDp.AutomationId != automationId) {
                //var tb = window.FindChildByControlTypeAndAutomationId(ControlType.Calendar, automationId);
                var d = window.FindChildByClassAndAutomationId("DatePicker", automationId);
                _cachedDp = new GuiDatePicker(d);
            }
            return _cachedDp;
        }

        public static GuiDatePicker GetDatePickerExtendedC(AutomationElement window, string automationId) {
            if (_cachedDp == null || _cachedDp.AutomationId != automationId) {
                //var tb = window.FindChildByControlTypeAndAutomationId(ControlType.Calendar, automationId);
                var children = window.FindAllChildrenByAutomationId(automationId).ToList();
                PrintControls(children);
                var d = children.First(c => c.Current.ClassName == "DatePicker");
                _cachedDp = new GuiDatePicker(d);
            }
            return _cachedDp;
        }

        public static void InvalidateCache() {
            _cachedDp = null;
        }

        public string AutomationId { get; private set; }

        public GuiDatePicker(AutomationElement datepicker) : base(datepicker) {
            AutomationId = datepicker.Current.AutomationId;
        }

        public void SetText(DateTime? value) {
            GuiTextBox.InvalidateCache();
            if(value.HasValue)
                TextBoxC("PART_TextBox").SetText(value.Value.ToShortDateString());
            else
                TextBoxC("PART_TextBox").SetText("");
        }

        public void ShouldRead(DateTime? value){
            GuiTextBox.InvalidateCache();
            if(value.HasValue)
                TextBoxC("PART_TextBox").ShouldRead(value.Value.ToShortDateString());
            else
                TextBoxC("PART_TextBox").ShouldRead("");
        }

        public void SetToDaysFromNow(int days) {
            SetText((DateTime.Now + TimeSpan.FromDays(days)).Date);
        }

        public void ShouldBeSetToDaysFromNow(int days) {
            ShouldRead((DateTime.Now + TimeSpan.FromDays(days)).Date);
        }

        public void ShouldBeEditable() {
            GuiTextBox.InvalidateCache();
            TextBoxC("PART_TextBox").AssertIsEditable();
        }

        public void ShouldNotBeEditable() {
            GuiTextBox.InvalidateCache();
            TextBoxC("PART_TextBox").AssertIsNotEditable();
        }
    }
}
