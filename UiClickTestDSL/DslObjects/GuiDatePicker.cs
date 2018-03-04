using System;
using System.Windows.Automation;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiDatePicker : GuiUserControl {
        private static GuiDatePicker _cachedDp = null;
        public static GuiDatePicker GetDatePicker(AutomationElement window, string automationId) {
            if (_cachedDp == null || _cachedDp.AutomationId != automationId) {
                //var tb = window.FindChildByControlTypeAndAutomationId(ControlType.Calendar, automationId);
                var d = window.FindChildByClassAndAutomationId("DatePicker", automationId);
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
                TextBox("PART_TextBox").SetText(value.Value.ToShortDateString());
            else
                TextBox("PART_TextBox").SetText("");
        }

        public void ShouldRead(DateTime? value){
            GuiTextBox.InvalidateCache();
            if(value.HasValue)
                TextBox("PART_TextBox").ShouldRead(value.Value.ToShortDateString());
            else
                TextBox("PART_TextBox").ShouldRead("");
        }

        public void SetToDaysFromNow(int days) {
            SetText((DateTime.Now + TimeSpan.FromDays(days)).Date);
        }

        public void ShouldBeSetToDaysFromNow(int days) {
            ShouldRead((DateTime.Now + TimeSpan.FromDays(days)).Date);
        }

        public void ShouldBeEditable() {
            GuiTextBox.InvalidateCache();
            TextBox("PART_TextBox").AssertIsEditable();
        }

        public void ShouldNotBeEditable() {
            GuiTextBox.InvalidateCache();
            TextBox("PART_TextBox").AssertIsNotEditable();
        }
    }
}
