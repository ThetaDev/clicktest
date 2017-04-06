using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiListBox {
        private static ILog Log = LogManager.GetLogger(typeof(GuiListBox));

        public AutomationElement InternalElement { get; private set; }
        private readonly string _automationId;

        public GuiListBox(AutomationElement listBox, string automationId) {
            InternalElement = listBox;
            _automationId = automationId;
        }

        public static GuiListBox Find(AutomationElement window, string automationId) {
            AutomationElement res = window.FindChildByControlTypeAndAutomationIdOrName(ControlType.List, automationId);
            return new GuiListBox(res, automationId);
        }

        public void TrySetFocus() {
            try {
                UiTestDslCoreCommon.RepeatTryingFor(TimeSpan.FromSeconds(20), () => {
                    InternalElement.SetFocus();
                });
            } catch (Exception ex) {
                //todo cannot set focus
                Log.Debug("Unable to set focus to " + _automationId, ex);
            }
        }

        public List<GuiListBoxItem> GetAllListItems() {
            TrySetFocus();
            IEnumerable<AutomationElement> all = InternalElement.FindAllChildrenByControlType(ControlType.ListItem);
            return all.Select(listItem => new GuiListBoxItem(listItem)).ToList();
        }

        public List<AutomationElement> GetChildItems() {
            TrySetFocus();
            IEnumerable<AutomationElement> all = InternalElement.FindChildrenByControlType(ControlType.ListItem);
            return all.ToList(); // .Select(listItem => new GuiListBoxItem(listItem))
        }

        public List<ListUiItem> GetAllUiItems() {
            TrySetFocus();
            IEnumerable<AutomationElement> all = InternalElement.FindAllChildrenByClassName("UIItem");
            return all.Select(listItem => new ListUiItem(listItem)).ToList();
        }

        public void PrintAllControls() {
            UiTestDslCoreCommon.PrintAllControls(InternalElement);
        }

        public GuiListBoxItem GetFirstElement() {
            return GetAllListItems()[0];
        }

        public GuiListBoxItem this[int i] {
            get { return GetAllListItems()[i]; }
        }

        public void ShouldContainButton(string elementName) {
            IList<GuiListBoxItem> all = GetAllListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasButtonWithText(elementName)
                                                select i;
            Assert.AreEqual(1, items.Count());
        }


        public void CountShouldBe(int expectedCount) {
            Assert.AreEqual(expectedCount, GetAllListItems().Count);
        }

        public GuiListBoxItem SelectLastItem() {
            var all = GetAllListItems();
            var item = all[all.Count - 1];
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public GuiListBoxItem SelectFirstItem() {
            var all = GetAllListItems();
            var item = all[0];
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public GuiListBoxItem SelectItemByIndex(int index) {
            var item = this[index];
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public GuiListBoxItem SelectElementWithLabel(string value) {
            IList<GuiListBoxItem> all = GetAllListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(null, value)
                                                select i;
            GuiListBoxItem item = items.FirstOrDefault();
            if (item == null) {
                //try to scroll to the bottom, to see if we can find it there.
                try {
                    var scroll = InternalElement.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                    scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 100);
                } catch (InvalidOperationException) {
                    //This means there was no scrollbar because the list in the TreeView is to short to be scrollable   
                }
                all = GetAllListItems();
                items = from i in all
                        where i.HasLabelWithText(null, value)
                        select i;
                item = items.FirstOrDefault();
            }
            if (item == null) {
                var allLabels = from i in all
                                from l in i.GetLabels(null)
                                select l.Text;
                var labelsStr = string.Join(Environment.NewLine, allLabels);
                var msg = $"Unable to find element with label, even after scrolling to the bottom. Searching for \"{value}\". Found: {Environment.NewLine}{labelsStr}";
                UiTestDslCoreCommon.PrintLine(msg);
            }
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public void ElementNotInList(string elementName, string value) {
            IList<GuiListBoxItem> all = GetAllListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(elementName, value)
                                                select i;
            Assert.AreEqual(0, items.Count());
        }

        /// <summary>
        /// Does not appear to work properly with only .Net 4.0 installed, but works fine with .Net 4.5 installed.
        /// </summary>
        private GuiListBoxItem SelectFirstMatch(string match) {
            var all = GetAllListItems();
            var guiListBoxItem = all.FirstOrDefault(i => i.HasLabelStartingWithText(match));
            if (guiListBoxItem == null)
                throw new Exception("Can't find any ListBoxItem starting with: " + match);
            guiListBoxItem.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return guiListBoxItem;
        }
    }
}
