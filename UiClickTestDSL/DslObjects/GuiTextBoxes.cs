using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UiClickTestDSL.DslObjects {
    public class GuiTextBoxes : List<GuiTextBox> {
        public static GuiTextBoxes GetAll(AutomationElement window, string prefix) {
            var all = GuiTextBox.GetAll(window);
            if (prefix == "") {
                return new GuiTextBoxes(all.Select(tb => new GuiTextBox(tb)));
            }
            var tbs = from l in all
                      where l.Current.AutomationId.StartsWith(prefix)
                      select new GuiTextBox(l);
            return new GuiTextBoxes(tbs);
        }

        public GuiTextBoxes(IEnumerable<GuiTextBox> tbList)
            : base(tbList) {
        }

        private IEnumerable<GuiTextBox> GetVisible(string except = "") {
            var visible = from t in this
                          where t.Visible && !except.Contains(t.AutomationId)
                          select t;
            UiTestDslCoreCommon.PrintLine("Visible: " + visible.Aggregate("", (current, tb) => current + tb.AutomationId + ": " + tb.Text + ", "));
            return visible;
        }

        public void AllShouldBeEmpty(string except = "") {
            var nonEmpty = from t in this
                           where !String.IsNullOrWhiteSpace(t.Text) && !except.Contains(t.AutomationId)
                           select t;
            Assert.IsTrue(nonEmpty.Count() == 0);
        }

        public void AllShouldHaveContent(string except = "") {
            IEnumerable<GuiTextBox> nonEmpty = from t in this
                                               where t.Visible && (!String.IsNullOrWhiteSpace(t.Text) || !except.Contains(t.AutomationId))
                                               select t;
            Assert.AreEqual(nonEmpty.Count(), GetVisible(except).Count(), nonEmpty.Aggregate("\n", (current, tb) => current + tb.AutomationId + ": " + tb.Text + "\n "));
        }

        public void AllShouldBeEnabled(string except = "") {
            IEnumerable<GuiTextBox> enabled = from t in this
                                              where t.IsEditable && !except.Contains(t.AutomationId)
                                              select t;
            Assert.IsTrue(enabled.Count() == GetVisible(except).Count());
        }

        public void AllShouldBeDisabled(string except = "") {
            IEnumerable<GuiTextBox> enabled = from t in this
                                              where t.IsEditable && !except.Contains(t.AutomationId)
                                              select t;
            Assert.IsTrue(enabled.Count() == 0);
        }

        public void CountShouldBe(int expectedCount) {
            Assert.AreEqual(expectedCount, Count);
        }

        public void SetValues(string[] selectValues) {
            for (int i = 0; i < selectValues.Count(); i++) {
                this[i].SetText(selectValues[i]);
            }
        }
    }
}
