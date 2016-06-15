using System.Collections.Generic;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiTabItem : UiTestDslCoreCommon {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var res = window.FindAllChildrenByControlType(ControlType.TabItem);
            return res;
        }

        private static GuiTabItem _cachedTab = null;
        private static AutomationElement _currentParentWindow = null;

        public static GuiTabItem GetTabByAutomationId(AutomationElement parentWindow, string automationId) {
            if (_cachedTab == null || _cachedTab.AutomationId != automationId) {
                var res = parentWindow.FindChildByControlTypeAndAutomationId(ControlType.TabItem, automationId);
                _cachedTab = new GuiTabItem(res, automationId);
                _currentParentWindow = parentWindow;
            }
            return _cachedTab;
        }

        public static GuiTabItem GetTabByName(AutomationElement parentWindow, string name) {
            if (_cachedTab == null || _cachedTab.AutomationId != name) {
                var res = parentWindow.FindChildByControlTypeAndName(ControlType.TabItem, name);
                _cachedTab = new GuiTabItem(res, name);
                _currentParentWindow = parentWindow;
            }
            return _cachedTab;
        }

        public static void InvalidateCache() {
            _cachedTab = null;
        }

        public static void ShouldExist(AutomationElement window, string automationId) {
            GuiTabItem tabItem = null;
            try {
                tabItem = GetTabByAutomationId(window, automationId);
            } catch { }
            Assert.IsNotNull(tabItem);
            Assert.AreEqual(automationId, tabItem.AutomationId);
        }

        private SelectionItemPattern _selection;
        public string AutomationId { get; private set; }
        public bool IsSelected { get { return _selection.Current.IsSelected; } }

        private GuiTabItem(AutomationElement ti, string automationId) {
            Window = ti;
            _selection = Window.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            AutomationId = automationId;
        }

        public void Select() {
            //selection = window.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            if (!_selection.Current.IsSelected)
                _selection.Select();
        }

        public override void GetThisWindow() {
            _cachedTab = GetTabByAutomationId(_currentParentWindow, AutomationId);
        }

        public void VerifyIsCurrentTab() {
            _selection = Window.GetPattern<SelectionItemPattern>(SelectionItemPattern.Pattern);
            Assert.IsTrue(_selection.Current.IsSelected);
        }
    }
}
