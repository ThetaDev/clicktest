using System.Windows.Automation;
using System.Windows;
using Microsoft.Test.Input;
using UiClickTestDSL.AutomationCode;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using UiClickTestDSL.DslObjects;
using System.Windows.Input;

namespace UiClickTestDSL.DslObjects {

    public class GuiContextMenu {
        public static GuiContextMenu GetActive(AutomationElement window) {
            var res = window.FindChildByClass("ContextMenu");
            return new GuiContextMenu(res);
        }

        private readonly AutomationElement _element;
        

        public GuiContextMenu(AutomationElement ae) {
            _element = ae;
        }

        public void ClickFirstElement() {
            var point = _element.Current.BoundingRectangle.Location;
            var click = new System.Drawing.Point((int)point.X+8, (int)point.Y+8);
            Mouse.MoveTo(click);
            Mouse.Click(MouseButton.Left);
        }


        public void LeftClickElement(string elementname) {
            var all = _element.FindAllChildrenByName(elementname);
            foreach (var element in all) {
                System.Windows.Point clickablePoint = element.GetClickablePoint();
            Mouse.MoveTo(new System.Drawing.Point((int) clickablePoint.X, (int) clickablePoint.Y));
            Mouse.Click(MouseButton.Left);

            }
        }
        }
    }
