using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UiClickTestDSL.DslObjects {
    public class GuiLabels : List<GuiLabel> {
        public static GuiLabels GetAll(AutomationElement window, string prefix = null) {
            var all = GuiLabel.GetAll(window, prefix);
            return new GuiLabels(all.Select(l => new GuiLabel(l)));
        }

        public GuiLabels(IEnumerable<GuiLabel> lblList)
            : base(lblList) {
        }

        public void PrintVisibleLabels() {
            var lbls = from l in this
                       where l.Visible
                       select l.LabelElement;
            UiTestDslCoreCommon.PrintControls(lbls);
        }

        public bool VisibleContains(string text) {
            var lbls = from l in this
                       where l.Text == text && l.Visible
                       select l;
            return lbls.Count() > 0;
        }

        public int NumberOfVisible {
            get {
                return (from l in this
                        where l.Visible
                        select l).Count();
            }
        }

        public void ShouldBeVisible(string labelText) {
            Assert.IsTrue(VisibleContains(labelText), "Visible label is missing: " + labelText);
        }

        public void ShouldNotBeDisplayed(string labelText) {
            Assert.IsFalse(VisibleContains(labelText), "Visible labels should not contain: " + labelText);
        }
    }
}
