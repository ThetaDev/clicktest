using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using Microsoft.Test.Input;
using System.Text;
using UiClickTestDSL.DslObjects;



namespace UiClickTestDSL.DslObjects {
    public class GuiListBox {
        private static ILog Log = LogManager.GetLogger(typeof(GuiListBox));
        public AutomationElement Window;
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
        public static GuiListBox InvalidateCache() {
            AutomationElement res = null;
            return new GuiListBox(res, null);
        }


        public virtual GuiImage Image(string automationId) { return GuiImage.Find(Window, automationId); }

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

        /// <summary>
        /// Note: This method returns all descendants of type ListItem, and might not work as expected for nested listboxes.
        /// </summary>
        public List<GuiListBoxItem> GetAllListItems() {
            List<AutomationElement> all = GetChildItems();
            return all.Select(listItem => new GuiListBoxItem(listItem)).ToList();
        }

        /// <summary>
        /// Note: This method returns only direct children of type ListItem.
        /// </summary>
        public List<GuiListBoxItem> GetChildListItems() {
            List<AutomationElement> all = GetChildItems();
            return all.Select(listItem => new GuiListBoxItem(listItem)).ToList();
        }

        public List<GuiListBoxItem> GetChildCheckBoxItems() {
            TrySetFocus();
            ScrollAllItemsIntoView();
            IEnumerable<AutomationElement> all = InternalElement.FindChildrenByControlType(ControlType.CheckBox);
            return all.Select(GuiCheckBox => new GuiListBoxItem(GuiCheckBox)).ToList();
        }


        public List<AutomationElement> GetChildItems() {
            TrySetFocus();
            ScrollAllItemsIntoView();
            IEnumerable<AutomationElement> all = InternalElement.FindChildrenByControlType(ControlType.ListItem);
            return all.ToList(); // .Select(listItem => new GuiListBoxItem(listItem))
        }

        public List<ListUiItem> GetAllUiItems() {
            TrySetFocus();
            ScrollAllItemsIntoView();
            IEnumerable<AutomationElement> all = InternalElement.FindAllChildrenByClassName("UIItem");
            return all.Select(listItem => new ListUiItem(listItem)).ToList();
        }

        public void PrintAllControls() {
            UiTestDslCoreCommon.PrintAllControls(InternalElement);
        }

        public GuiListBoxItem GetFirstElement() {
            return GetChildListItems()[0];
        }

        public GuiListBoxItem this[int i] {
            get { return GetChildListItems()[i]; }
        }

        public void ShouldContainButton(string buttonName) {
            IList<GuiListBoxItem> all = GetChildListItems();
            PrintAllControls();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasButtonWithText(buttonName)
                                                select i;
            Assert.AreEqual(1, items.Count());
        }

        public void ShouldContainImageByAutomationId(string imageNameId) {
            IList<GuiListBoxItem> all = GetChildListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasImgWithId(imageNameId)
                                                select i;
            Assert.AreEqual(1, items.Count());
        }

        public void ShouldContainButtonByAutomationId(string buttonNameId) {
            IList<GuiListBoxItem> all = GetChildListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasButtonWithId(buttonNameId)
                                                select i;
            Assert.AreEqual(1, items.Count());
        }
        
        public void ShouldContainNoOfButtonsByAutomationId(string buttonNameId, int noofbuttons) {
            IList<GuiListBoxItem> all = GetChildListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasButtonWithId(buttonNameId)
                                                select i;
            Assert.AreEqual(noofbuttons, items.Count());
        }

        public void ShouldContainLabelWithText(string labelText) {
            IList<GuiListBoxItem> all = GetChildListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(text: labelText)
                                                select i;
            Assert.AreEqual(1, items.Count());
        }

        public void ShouldNotContainLabelWithText(string labelText) {
            IList<GuiListBoxItem> all = GetChildListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(text: labelText)
                                                select i;
            Assert.AreEqual(0, items.Count());
        }

        public void ShouldContainTheseLabels(params string[] txtValues) {
            IList<GuiListBoxItem> all = GetChildListItems();
            var missing = "";
            foreach (var txt in txtValues) {
                var el = all.FirstOrDefault(i => i.HasLabelWithText(text: txt));
                if (el == null)
                    missing += txt + ",";
            }
            Assert.IsTrue(string.IsNullOrWhiteSpace(missing), missing);
        }

        public void CountShouldBe(int expectedCount) {
            Assert.AreEqual(expectedCount, GetChildListItems().Count);
        }

        public GuiListBoxItem SelectLastItem() {
            var all = GetChildListItems();
            var item = all[all.Count - 1];
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public GuiListBoxItem SelectFirstItem() {
            var all = GetChildListItems();
            var item = all[0];
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public GuiListBoxItem SelectFirstElementWithImageByAutomationId(string imageid) {
            var all = GetChildListItems();
            //var item = all[0]; //(AutomationId(imageid));
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasImgWithId(imageid)
                                                where i.ImageIsNotOffscreen()
                                                select i;
            GuiListBoxItem item = items.FirstOrDefault();
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

        public void ScrollAllItemsIntoView(int intermediateSteps = 8) {
            if (intermediateSteps < 0 || intermediateSteps > 99)
                Assert.Fail("Parameter intermediateSteps has to be between 0 and 98");
            var stepLength = (int)(100 / (intermediateSteps + 1));
            try {
                var scroll = InternalElement.GetPattern<ScrollPattern>(ScrollPattern.Pattern);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 0);
                int scrollPercent = 0;
                for (int i = 0; i < intermediateSteps; i++) {
                    scrollPercent += stepLength;
                    scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: scrollPercent);
                    UiTestDslCoreCommon.SleepMilliseconds(50);
                }
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 100);
                scroll.SetScrollPercent(horizontalPercent: ScrollPattern.NoScroll, verticalPercent: 0); //set back to start
            } catch (InvalidOperationException) {
                //This means there was no scrollbar because the list in the TreeView is to short to be scrollable   
            }
        }

        public GuiListBoxItem SelectElementWithLabel(string value, bool debug = false) {
            List<GuiListBoxItem> completeSet = new List<GuiListBoxItem>();
            List<GuiListBoxItem> all = GetChildListItems();
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
                ScrollAllItemsIntoView();
                all = GetChildListItems();
                Log.Debug("Found elements: " + all.Count);
                completeSet.AddRange(all);
                items = from i in all
                        where i.HasLabelWithText(text: value)
                        select i;
                item = items.FirstOrDefault();
            }
            if (item == null) {
                var allLabels = completeSet.Distinct().Select(l => "[" + string.Join("\t", l.GetLabels(null)) + "]").ToList();
                var labelsStr = string.Join(Environment.NewLine, allLabels);
                var msg = $"Unable to find element with label, even after scrolling all items into view. Searching for \"{value}\". Found ({allLabels.Count} items): {Environment.NewLine}{labelsStr}";
                UiTestDslCoreCommon.PrintLine(msg);
            }
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public void SelectElementContainingLabelStartingWithText(string value, bool debug = false) {
            List<GuiListBoxItem> completeSet = new List<GuiListBoxItem>();
            List<GuiListBoxItem> all = GetChildListItems();
            Log.Debug("Found elements: " + all.Count);
            completeSet.AddRange(all);
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelStartingWithText(value)
                                                select i;
            GuiListBoxItem item = items.FirstOrDefault();
            if (item == null) {
                if (debug) {
                    var screenshotNo = ScreenShooter.SaveToFile();
                    Log.Debug("Saved screenshot of view of listbox before scrolling: " + screenshotNo);
                }
                //try to scroll to the bottom, to see if we can find it there.
                ScrollAllItemsIntoView();
                all = GetChildListItems();
                Log.Debug("Found elements: " + all.Count);
                completeSet.AddRange(all);
                items = from i in all
                        where i.HasLabelWithText(text: value)
                        select i;
                item = items.FirstOrDefault();
            }
            if (item == null) {
                var allLabels = completeSet.Distinct().Select(l => "[" + string.Join("\t", l.GetLabels(null)) + "]").ToList();
                var labelsStr = string.Join(Environment.NewLine, allLabels);
                var msg = $"Unable to find element with label, even after scrolling all items into view. Searching for \"{value}\". Found ({allLabels.Count} items): {Environment.NewLine}{labelsStr}";
                UiTestDslCoreCommon.PrintLine(msg);
            }
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
        }


        public void ElementNotInList(string elementName, string value) {
            IList<GuiListBoxItem> all = GetChildListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(elementName, value)
                                                select i;
            Assert.AreEqual(0, items.Count());
        }

        public void VerifyCountOfElementsWithValueInList(string elementName, string value, int countno) {
            IList<GuiListBoxItem> all = GetChildListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(elementName, value)
                                                select i;
            Assert.AreEqual(countno, items.Count());
        }

        public GuiListBoxItem SelectByTwoElementValuesInList(string elementname1, string value1, string elementname2, string value2, bool debug = false) {
            IList<GuiListBoxItem> all = GetChildListItems();
            List<GuiListBoxItem> completeSet = new List<GuiListBoxItem>();
            Log.Debug("Found elements: " + all.Count);
            completeSet.AddRange(all);
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(elementname1, value1)
                                                where i.HasLabelWithText(elementname2, value2)
                                                select i;
            GuiListBoxItem item = items.FirstOrDefault();

            /*
            if (item == null) {
                if (debug) {
                    var screenshotNo = ScreenShooter.SaveToFile();
                    Log.Debug("Saved screenshot of view of listbox before scrolling: " + screenshotNo);
                }
                //try to scroll to the bottom, to see if we can find it there.
                ScrollAllItemsIntoView();
                all = GetChildListItems();
                Log.Debug("Found elements: " + all.Count);
                completeSet.AddRange(all);
                items = from i in all
                        where i.HasLabelWithText(elementname1, value1)
                        where i.HasLabelWithText(elementname2, value2)
                        select i;
                item = items.FirstOrDefault();
            }
            */
            if (item == null) {
                var allLabels = completeSet.Distinct().Select(l => "[" + string.Join("\t", l.GetLabels(null)) + "]").ToList();
                var labelsStr = string.Join(Environment.NewLine, allLabels);
                var msg = $"Unable to find item with \"{elementname1}\" \"{value1}\" and \"{elementname2}\" \"{value2}\" , even after scrolling all items into view. Searching for \"{elementname1}\" \"{value1}\" and \"{elementname2}\" \"{value2}\". Found ({allLabels.Count} items): {Environment.NewLine}{labelsStr}";
                UiTestDslCoreCommon.PrintLine(msg);
            }

            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        public GuiListBoxItem SelectByOneElementValueInList(string elementName, string value, bool debug = false) {
            List<GuiListBoxItem> completeSet = new List<GuiListBoxItem>();
            List<GuiListBoxItem> all = GetChildListItems();
            Log.Debug("Found elements: " + all.Count);
            completeSet.AddRange(all);
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(elementName, value)
                                                select i;
            GuiListBoxItem item = items.FirstOrDefault();
            /*
            if (item == null) {
                if (debug) {
                    var screenshotNo = ScreenShooter.SaveToFile();
                    Log.Debug("Saved screenshot of view of listbox before scrolling: " + screenshotNo);
                }
                //try to scroll to the bottom, to see if we can find it there.
                ScrollAllItemsIntoView();
                all = GetChildListItems();
                Log.Debug("Found elements: " + all.Count);
                completeSet.AddRange(all);
                items = from i in all
                        where i.HasLabelWithText(elementName, value)
                        select i;
                item = items.FirstOrDefault();
            }
            */
            if (item == null) {
                var allLabels = completeSet.Distinct().Select(l => "[" + string.Join("\t", l.GetLabels(null)) + "]").ToList();
                var labelsStr = string.Join(Environment.NewLine, allLabels);
                var msg = $"Unable to find element with label, even after scrolling all items into view. Searching for \"{value}\". Found ({allLabels.Count} items): {Environment.NewLine}{labelsStr}";
                UiTestDslCoreCommon.PrintLine(msg);
            }
            item.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return item;
        }

        /// <summary>
        /// Does not appear to work properly with only .Net 4.0 installed, but works fine with .Net 4.5 installed.
        /// </summary>
        private GuiListBoxItem SelectFirstMatch(string match) {
            ScrollAllItemsIntoView();
            var all = GetChildListItems();
            var guiListBoxItem = all.FirstOrDefault(i => i.HasLabelStartingWithText(match));
            if (guiListBoxItem == null)
                throw new Exception("Can't find any ListBoxItem starting with: " + match);
            guiListBoxItem.Select();
            UiTestDslCoreCommon.WaitWhileBusy();
            return guiListBoxItem;
        }

        public void ItemShouldRead(int listIndex, string value) {
            var all = GetChildListItems();
            var item = all[listIndex];
            var guiListBoxItem = all.FirstOrDefault(i => i.HasLabelStartingWithText(value));
            if (guiListBoxItem == null)
                throw new Exception("Can't find any ListBoxItem starting with: " + value);
            item.HasLabelStartingWithText(value);
        }

        public void RightClickMouse(int listIndex, string value) {
            var all = GetChildListItems();
            var item = all[listIndex];
            var guiListBoxItem = all.FirstOrDefault(i => i.HasLabelStartingWithText(value));
            if (guiListBoxItem == null)
                throw new Exception("Can't find any ListBoxItem starting with: " + value);
            item.MoveMouseHere();
            Mouse.Click(MouseButton.Right); ;
        }

        public void ListShouldContainTheseItems(params string[] txtValues) {
            var all = GetChildListItems();
            var missing = "";
            foreach (var txt in txtValues) {
                var el = all.FirstOrDefault(i => i.HasLabelWithText(text: txt));
                if (el == null)
                    missing += txt + ",";
            }
            Assert.IsTrue(string.IsNullOrWhiteSpace(missing), missing);
        }
    }
}
