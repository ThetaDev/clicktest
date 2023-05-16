﻿using System;
using System.Collections.Generic;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using System.Linq;
using System.Text;
using System.Threading;

namespace UiClickTestDSL.DslObjects {
    public class GuiExpander {

        public AutomationElement Window;
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var tbs = window.FindAllChildrenByControlType(ControlType.Group);
            return tbs;
        }

        public static GuiExpander GetExpanderByAutomationId(AutomationElement window, string automationId) {
            var res = window.FindChildByControlTypeAndAutomationId(ControlType.Group, automationId);
            return new GuiExpander(res, automationId);
        }

        public static GuiExpander GetExpander(AutomationElement window, string name) {
            AutomationElement res = window.FindChildByControlTypeAndName(ControlType.Group, name);
            return new GuiExpander(res, name);
        }

        private readonly AutomationElement _expander;
        private readonly string _name;

        public GuiExpander(AutomationElement expanderFound, string name) {
            _expander = expanderFound;
            _name = name;
        }

        public void Click() {
            ClickNoWait();
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        public void ClickNoWait() {
            var invoker = _expander.GetPattern<ExpandCollapsePattern>(ExpandCollapsePattern.Pattern);
            if (invoker.Current.ExpandCollapseState == ExpandCollapseState.Collapsed)
                invoker.Expand();
            else if (invoker.Current.ExpandCollapseState == ExpandCollapseState.Expanded)
                invoker.Collapse();
        }

        public void Expand() {
            ExpandNoWait();
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        public void ExpandNoWait() {
            var invoker = _expander.GetPattern<ExpandCollapsePattern>(ExpandCollapsePattern.Pattern);
            invoker.Expand();
        }

        public void Collapse() {
            CollapseNoWait();
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        public void CollapseNoWait() {
            var invoker = _expander.GetPattern<ExpandCollapsePattern>(ExpandCollapsePattern.Pattern);
            invoker.Collapse();
        }

        public void ShouldBeDisabled() {
            Assert.IsFalse(_expander.Current.IsEnabled, _name + " was not disabled.");
        }

        public void ShouldBeEnabled() {
            Assert.IsTrue(_expander.Current.IsEnabled, _name + " was disabled.");
        }

        public void ShouldNotBeVisible() {
            Assert.IsTrue(_expander.Current.IsOffscreen, "Expander: " + _name + " should have been offscreen");
        }

        public void ShouldBeVisible() {
            Assert.IsFalse(_expander.Current.IsOffscreen, "Expander: " + _name + " should have been visible");
        }

        public void ShouldBeCollapsed() {
            var pattern = _expander.GetPattern<ExpandCollapsePattern>(ExpandCollapsePattern.Pattern);
            var state = pattern.Current.ExpandCollapseState;
            Assert.AreEqual(ExpandCollapseState.Collapsed, pattern.Current.ExpandCollapseState, $"Expander: {_name} should have been collapsed, but was {pattern.Current.ExpandCollapseState.ToString()}");
        }

        public void ShouldBeExpanded() {
            var pattern = _expander.GetPattern<ExpandCollapsePattern>(ExpandCollapsePattern.Pattern);
            Assert.AreEqual(ExpandCollapseState.Expanded, pattern.Current.ExpandCollapseState, $"Expander: {_name} should have been expanded, but was {pattern.Current.ExpandCollapseState.ToString()}");
        }

        public virtual GuiListBox ListBox(string name) { return GuiListBox.Find(Window, name); }
        public virtual GuiTextBoxes TextBoxes(string prefix = "") { return GuiTextBoxes.GetAll(Window, prefix); }
        public virtual GuiTextBox TextBoxC(string automationId) { return GuiTextBox.GetTextBoxC(Window, automationId); }
    }
}
