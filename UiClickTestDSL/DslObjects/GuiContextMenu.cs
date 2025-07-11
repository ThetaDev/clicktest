﻿using System.Windows.Automation;
using Microsoft.Test.Input;
using UiClickTestDSL.AutomationCode;
using System.Linq;

namespace UiClickTestDSL.DslObjects {
    public class GuiContextMenu {
        public static GuiContextMenu GetActive(AutomationElement window) {
            var res = window.FindChildByClass("ContextMenu");
            return new GuiContextMenu(res);
        }

        private readonly AutomationElement _element;

        public AutomationElement InternalElement { get; private set; }

        public GuiContextMenu(AutomationElement ae) {
            _element = ae;
        }

        public void ClickFirstElement() {
            var point = _element.Current.BoundingRectangle.Location;
            var click = new System.Drawing.Point((int)point.X + 8, (int)point.Y + 8);
            Mouse.MoveTo(click);
            Mouse.Click(MouseButton.Left);
        }

        public void LeftClickElement(string elementname) {
            var all = _element.FindAllChildrenByName(elementname);
            foreach (var element in all) {
                System.Windows.Point clickablePoint = element.GetClickablePoint();
                Mouse.MoveTo(new System.Drawing.Point((int)clickablePoint.X, (int)clickablePoint.Y));
                Mouse.Click(MouseButton.Left);
            }
        }

        public void RightClickElement(string elementname) {
            var all = _element.FindAllChildrenByName(elementname);
            foreach (var element in all) {
                System.Windows.Point clickablePoint = element.GetClickablePoint();
                Mouse.MoveTo(new System.Drawing.Point((int)clickablePoint.X, (int)clickablePoint.Y));
                Mouse.Click(MouseButton.Right);
            }
        }

        public void MoveMouseToElement(string elementname) {
            var all = _element.FindAllChildrenByName(elementname);
            foreach (var element in all) {
                System.Windows.Point clickablePoint = element.GetClickablePoint();
                Mouse.MoveTo(new System.Drawing.Point((int)clickablePoint.X, (int)clickablePoint.Y));
            }
        }


        public void MouseScrollToBottom() {
            var all = _element.FindAllChildrenByAutomationId("LineDown");
            var allCount = all.Count();
            if (allCount == 0) {
                return;
            }
            foreach (var element in all) {
                System.Windows.Point clickablePoint = element.GetClickablePoint();
                Mouse.MoveTo(new System.Drawing.Point((int)clickablePoint.X, (int)clickablePoint.Y));
                Mouse.Click(MouseButton.Left);
            }
        }
    }
}
