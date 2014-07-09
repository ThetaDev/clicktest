using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiListBoxItem : GuiUserControl {
        private readonly SelectionItemPattern _selection;

        public GuiListBoxItem(AutomationElement item)
            : base(item) {
            _selection = Window.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
        }

        public bool IsSelected {
            get { return _selection.Current.IsSelected; }
        }

        public void Select() {
            _selection.Select();
        }

        public void OneLabelShouldHaveText(string text) {
            var labels = new List<GuiLabel>();
            AutomationElement el = TreeWalker.RawViewWalker.GetFirstChild(Window);
            while (el != null) {
                if (el.Current.ControlType == ControlType.Text)
                    labels.Add(new GuiLabel(el));
                el = TreeWalker.RawViewWalker.GetNextSibling(el);
            }
            var contains = from l in labels
                           where l.Text == text
                           select l;
            Assert.AreNotEqual(0, contains.Count());
        }

        public bool HasLabelWithText(string labelName, string text) {
            return GuiLabels.GetAll(Window).VisibleContains(text);
        }
    }
}
