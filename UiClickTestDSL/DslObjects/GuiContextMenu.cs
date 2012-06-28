using System.Windows.Automation;
using Microsoft.Test.Input;
using UiClickTestDSL.AutomationCode;
using System.Drawing;

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
            var click = new Point((int)point.X+8, (int)point.Y+8);
            Mouse.MoveTo(click);
            Mouse.Click(MouseButton.Left);
        }
    }
}