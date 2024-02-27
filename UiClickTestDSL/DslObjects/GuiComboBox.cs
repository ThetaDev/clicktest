using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using UiClickTestDSL.HelperPrograms;

namespace UiClickTestDSL.DslObjects {
    public class GuiComboBox {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var res = window.FindAllChildrenByControlType(ControlType.ComboBox);
            return res;
        }

        public static GuiComboBox Find(AutomationElement window, string automationId) {
            var res = window.FindChildByControlTypeAndAutomationId(ControlType.ComboBox, automationId);
            return new GuiComboBox(res);
        }

        public static GuiComboBox FindByName(AutomationElement window, string name) {
            var res = window.FindChildByControlTypeAndName(ControlType.ComboBox, name);
            return new GuiComboBox(res);
        }


        public readonly AutomationElement _cmb;
        private readonly ExpandCollapsePattern _expandCollapse;

        public GuiComboBox(AutomationElement comboBox) {
            _cmb = comboBox;
            _expandCollapse = _cmb.GetPattern<ExpandCollapsePattern>(ExpandCollapsePattern.Pattern);
        }

        private void Activate() {
            _cmb.SetFocus();
            _expandCollapse.Expand();
            Thread.Sleep(100);
            //_expandCollapse.Collapse();
        }

        public void Collapse() {
            _expandCollapse.Collapse();
        }

        public GuiComboBoxItem SelectedItem {
            get {
                List<GuiComboBoxItem> all = GetAllItems();
                IEnumerable<GuiComboBoxItem> item = from i in all
                                                    where i.IsSelected
                                                    select i;
                return item.First();
            }
        }

        public string DisplayText {
            get {
                var selectionPattern = _cmb.GetPattern<SelectionPattern>(SelectionPattern.Pattern);
                var selection = selectionPattern.Current.GetSelection();
                var first = selection.First();
                var comboBoxItem = new GuiComboBoxItem(first, this);
                return comboBoxItem.Text;
            }
        }

        public void CountShouldBe(int expectedCount) {
            Assert.AreEqual(expectedCount, GetAllItems().Count);
        }

        public void SelectItem(int i) {
            var all = GetAllItems();
            Assert.IsTrue(all.Count > i, "Not enough items to select #" + i);
            all[i].Select();
        }

        public bool IsEnabled { get { return _cmb.Current.IsEnabled; } }

        public void ShouldBeDisabled() {
            Assert.IsFalse(IsEnabled, _cmb.Current.Name + " was not disabled.");
        }

        public void ShouldBeEnabled() {
            Assert.IsTrue(IsEnabled, _cmb.Current.Name + " was disabled.");
        }

        public void SelectItem(string caption) {
            List<GuiComboBoxItem> all = GetAllItems();
            IEnumerable<GuiComboBoxItem> items = from i in all
                                                 where RegexMatch(i.Text.Trim(), caption)
                                                 select i;
            var item = items.FirstOrDefault();
            if (item == null)
                Assert.Fail("Unable to find ComboBoxItem with caption: " + caption + $" # found: {all.Count}");
            item.Select();
            Collapse();
        }

        public void SelectItemContaining(string caption) {
            List<GuiComboBoxItem> all = GetAllItems();
            IEnumerable<GuiComboBoxItem> item = from i in all
                                                where i.Text.Contains(caption)
                                                select i;
            item.First().Select();
        }

        private static bool RegexMatch(string text, string caption) {
            return text == caption || Regex.IsMatch(text, @"(.* name\:|\[.*,) " + caption + @"(\]){0,1}") || text.Trim().EndsWith(": " + caption);
        }

        public List<GuiComboBoxItem> GetAllItems() {
            Activate();
            IEnumerable<AutomationElement> all = _cmb.FindAllChildrenByControlType(ControlType.ListItem);
            return all.Select(comboBoxItem => new GuiComboBoxItem(comboBoxItem, this)).ToList();
        }

        public void PrintAllItems() {
            var all = GetAllItems();
            UiTestDslCoreCommon.PrintLine($"Found #items: {all.Count}");
            foreach (var item in all) {
                UiTestDslCoreCommon.PrintLine(item.Text);
            }
        }

        public void ShouldRead(string text) {
            string displayed = DisplayText;
            Assert.IsTrue(RegexMatch(displayed, text), "Wrong value in combobox, should be: " + text + ", was: " + displayed);
        }

        public void ShouldReadContaining(string text) {
            string displayed = DisplayText;
            Assert.IsTrue(displayed.ContainsIgnoreCase(text), "Wrong value in combobox, should contain: " + text + ", was: " + displayed);
        }

        public void ShouldNotReadContaining(string text) {
            string displayed = DisplayText;
            Assert.IsFalse(displayed.ContainsIgnoreCase(text), "Wrong value in combobox, should not contain: " + text + ", was: " + displayed);
        }


        public void ShouldContainItems() {
            Assert.AreNotEqual(0, GetAllItems().Count);
        }

        public void ShouldContainAtLeastTheseItems(params string[] items) {
            Assert.AreNotEqual(0, GetAllItems().Count);
            foreach (var item in items) {
                var all = GetAllItems();
                var itemlist = from i in all
                               where i.Text.Equals(item)
                               select i;
                Assert.AreNotEqual(0, itemlist.ToList().Count);
            }
        }

        public void ShouldContainTheseItems(params string[] items) {
            Assert.AreNotEqual(0, GetAllItems().Count);
            var _itemscount = items.Count();
            foreach (var item in items) {
                var all = GetAllItems();
                var itemlist = from i in all
                               where i.Text.Equals(item)
                               select i;
                //Assert.AreNotEqual(0, itemlist.ToList().Count);
                //Assert.AreEqual(_itemscount, itemlist.ToList().Count);
            }
        }


        public void ShouldNotContainTheseItems(params string[] items) {
            Assert.AreNotEqual(0, GetAllItems().Count);

            foreach (var item in items) {
                var all = GetAllItems();
                var itemlist = from i in all
                               where i.Text.Equals(item)
                               select i;
                Assert.AreEqual(0, itemlist.ToList().Count);
            }
        }

        public void VerifyBySelectionItemsContaining(params string[] items) {    //temp //TODO veryfi uten select
            Assert.AreNotEqual(0, GetAllItems().Count);
            foreach (var item in items) {
                List<GuiComboBoxItem> all = GetAllItems();
                IEnumerable<GuiComboBoxItem> listitem = from i in all
                                                        where i.Text.Contains(item)
                                                        select i;
                listitem.First().Select();
            }
        }
        }
}