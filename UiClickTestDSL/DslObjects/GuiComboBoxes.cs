using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UiClickTestDSL.DslObjects {
    public class GuiComboBoxes : List<GuiComboBox> {
        public static GuiComboBoxes Find(AutomationElement window, string prefix) {
            if (_cachePrefix == prefix)
                return _cachedCmbs;

            var all = GuiComboBox.GetAll(window);
            if (prefix == "") {
                return new GuiComboBoxes(all.Select(tb => new GuiComboBox(tb)));
            }
            var tbs = from l in all
                      where l.Current.AutomationId.StartsWith(prefix)
                      select new GuiComboBox(l);
            return new GuiComboBoxes(tbs);
        }

        private static GuiComboBoxes _cachedCmbs = null;
        private static string _cachePrefix = null;

        public static void InvalidateCache() {
            _cachedCmbs = null;
            _cachePrefix = null;
        }


        public GuiComboBoxes(IEnumerable<GuiComboBox> list) : base(list) { }

        public void ShouldShow(params string[] values) {
            var displayedTexts = from cb in this
                                 select cb.DisplayText;

            var res = from text in values
                      where !displayedTexts.Contains(text)
                      select text;
            Assert.AreEqual(0, res.Count(), "Values that should be shown are missing: " + res.Aggregate("", (current, text) => current + text));
        }

        public void CountShouldBe(int expectedcount) {
            Assert.AreEqual(expectedcount, Count, "Expected number of ComboBoxes is wrong: " + expectedcount + " is: " + Count);
        }

        public void SetValues(string[] selectValues) {
            for (int i = 0; i < selectValues.Count(); i++) {
                this[i].SelectItem(selectValues[i]);
            }
        }
    }
}