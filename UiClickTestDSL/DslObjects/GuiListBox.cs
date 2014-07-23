﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiListBox {
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

        public List<GuiListBoxItem> GetAllListItems() {
            try {
                InternalElement.SetFocus();
            } catch (Exception) {
                //todo ta bare cannot set focus her
            }
            UiTestDslCoreCommon.PrintAllControls(InternalElement);
            IEnumerable<AutomationElement> all = InternalElement.FindAllChildrenByControlType(ControlType.ListItem);
            return all.Select(listItem => new GuiListBoxItem(listItem)).ToList();
        }

        public List<ListUiItem> GetAllUiItems() {
            try {
                InternalElement.SetFocus();
            } catch (Exception) {
                //todo ta bare cannot set focus her
            }
            UiTestDslCoreCommon.PrintAllControls(InternalElement);
            IEnumerable<AutomationElement> all = InternalElement.FindAllChildrenByClassName("UIItem");
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
