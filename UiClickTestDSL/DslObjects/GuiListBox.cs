using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiListBox {
        private readonly AutomationElement _element;

        public GuiListBox(AutomationElement listBox) {
            _element = listBox;
        }

        public static GuiListBox Find(AutomationElement window, string automationId) {
            AutomationElement res = window.FindChildByControlTypeAndAutomationIdOrName(ControlType.List, automationId);
            return new GuiListBox(res);
        }

        public List<GuiListBoxItem> GetAllListItems() {
            try {
                _element.SetFocus();
            } catch (Exception) {
                //todo ta bare cannot set focus her
            }
            UiTestDslCoreCommon.PrintAllControls(_element);
            IEnumerable<AutomationElement> all = _element.FindAllChildrenByControlType(ControlType.ListItem);
            return all.Select(listItem => new GuiListBoxItem(listItem)).ToList();
        }

        public List<ListUiItem> GetAllUiItems() {
            try {
                _element.SetFocus();
            } catch (Exception) {
                //todo ta bare cannot set focus her
            }
            UiTestDslCoreCommon.PrintAllControls(_element);
            IEnumerable<AutomationElement> all = _element.FindAllChildrenByClassName("UIItem");
            return all.Select(listItem => new ListUiItem(listItem)).ToList();
        }

        public GuiListBoxItem GetFirstElement() {
            return GetAllListItems()[0];
        }

        public GuiListBoxItem SelectElement(string elementName, string value)
        {
            IList<GuiListBoxItem> all = GetAllListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(elementName, value)
                                                select i;
            GuiListBoxItem item = items.FirstOrDefault();
            if (item == null) {
                var allLabels = from i in all
                                from l in i.GetLabels(elementName)
                                select l.Text;
                Console.Write("\nValue: ");
                Console.WriteLine(value);
                Console.WriteLine("All found:");
                foreach (var label in allLabels) {
                    Console.WriteLine(label);
                }
            }
            item.Select();
            return item;
        }

        public void ElementNotInList(string elementName, string value) {
            IList<GuiListBoxItem> all = GetAllListItems();
            IEnumerable<GuiListBoxItem> items = from i in all
                                                where i.HasLabelWithText(elementName, value)
                                                select i;
            Assert.AreEqual(0, items.Count());
        }
    }
}
