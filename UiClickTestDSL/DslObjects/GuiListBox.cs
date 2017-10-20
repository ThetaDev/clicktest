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
            if (!InternalElement.Current.IsKeyboardFocusable || InternalElement.Current.HasKeyboardFocus)
                return; //Check decompiled for SetFocus, it only tries to set KeyBoardFocus
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

        public void ShouldContainButton(string buttonName) {
            IList<GuiListBoxItem> all = GetAllListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasButtonWithText(buttonName)
                                                select i;
            Assert.AreEqual(1, items.Count());
        }

        public void ShouldContainLabelWithText(string labelText) {
            IList<GuiListBoxItem> all = GetAllListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(text: labelText)
                                                select i;
            Assert.AreEqual(1, items.Count());
        }

        public void ShouldContainTheseLabels(params string[] txtValues) {
            IList<GuiListBoxItem> all = GetAllListItems();
            var missing = "";
            foreach (var txt in txtValues) {
                var el = all.FirstOrDefault(i => i.HasLabelWithText(text: txt));
                if (el == null)
                    missing += txt + ",";
            }
            Assert.IsTrue(string.IsNullOrWhiteSpace(missing), missing);
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

        public void ScrollToTop() {
            try {
                var scroll = InternalElement.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 0);
            } catch (InvalidOperationException) {
                //This means there was no scrollbar because the list in the TreeView is to short to be scrollable   
            }
        }

        public void ScrollToBottom() {
            try {
                var scroll = InternalElement.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 100);
            } catch (InvalidOperationException) {
                //This means there was no scrollbar because the list in the TreeView is to short to be scrollable   
            }
        }

        public GuiListBoxItem SelectElementWithLabel(string value, bool debug = false) {
            List<GuiListBoxItem> completeSet = new List<GuiListBoxItem>();
            List<GuiListBoxItem> all = GetAllListItems();
            Log.Debug("Found elements: " + all.Count);
            completeSet.AddRange(all);
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(text: value)
                                                select i;
            GuiListBoxItem item = items.FirstOrDefault();
            if (item == null) {
                if (debug) {
                    var screenshotNo = ScreenShooter.SaveToFile();
                    Log.Debug("Saved screenshot of view of listbox before scrolling: " + screenshotNo);
                }
                //try to scroll to the bottom, to see if we can find it there.
                ScrollToBottom();
                all = GetAllListItems();
                Log.Debug("Found elements: " + all.Count);
                completeSet.AddRange(all);
                items = from i in all
                        where i.HasLabelWithText(text: value)
                        select i;
                item = items.FirstOrDefault();
            }
            if (item == null) {
                var allLabels = completeSet.Distinct().Select(l => string.Join("\t", l.GetLabels(null)));
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
