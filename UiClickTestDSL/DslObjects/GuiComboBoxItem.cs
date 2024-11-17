using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiComboBoxItem {
        private readonly AutomationElement _cmbItem;
        private readonly GuiComboBox _parent;
        private readonly SelectionItemPattern _selection;

        public GuiComboBoxItem(AutomationElement item, GuiComboBox parent) {
            _cmbItem = item;
            _parent = parent;
            _selection = _cmbItem.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
        }

        public void ShouldRead(string expected) {
            Assert.AreEqual(expected, Text, "ComboBoxItem has a wrong text.");
        }

        public string Text { get { return _cmbItem.Current.Name; } }

        public bool IsSelected {
            get { return _selection.Current.IsSelected; }
        }

        public void Select() {
            _selection.Select();
            _parent.Collapse();
        }

        public void SelectWithoutCollapse() {
            _selection.Select();
        }
    }
}
