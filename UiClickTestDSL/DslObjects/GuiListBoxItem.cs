using System;
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

        public void NameShouldBe(string text) {
            Assert.AreEqual(text, Window.Current.Name);
        }

        public void OneLabelShouldHaveText(string text) {
            var labels = new List<GuiLabel>();
            AutomationElement el = TreeWalker.RawViewWalker.GetFirstChild(Window);
            int i = 0;
            while (el != null) {
                i++;
                if (el.Current.ControlType == ControlType.Text)
                    labels.Add(new GuiLabel(el));
                el = TreeWalker.RawViewWalker.GetNextSibling(el);
            }
            var contains = from l in labels
                           where l.Text == text
                           select l;
            Assert.AreNotEqual(0, contains.Count(), "Expected text: " + text + "  ; # elements found; " + i + " ; Texts actually found: " + string.Join(Environment.NewLine, labels.Select(l => l.Text)));
        }

        public void VerifyHasLabelWithText(string labelAutomationId = null, string text = "value to search for") {
            Assert.IsTrue(HasLabelWithText(labelAutomationId, text), "No label found with text: " + text);
        }

        public bool HasLabelWithText(string labelAutomationId = null, string text = "value to search for") {
            return GuiLabels.GetAll(Window, labelAutomationId).VisibleContains(text);
        }
        public bool HasButtonWithText(string buttonName) {
            var all = GuiButton.GetAll(Window);
            return all.Any(btn => btn.Current.Name.Equals(buttonName));
        }

        public bool HasLabelStartingWithText(string buttonName) {
            var all = GuiLabel.GetAll(Window);
            return all.Any(i => i.Current.Name.StartsWith(buttonName));
        }
    }
}
