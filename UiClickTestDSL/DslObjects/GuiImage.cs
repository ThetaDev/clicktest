using System.Collections.Generic;
using System.Windows.Automation;
using Microsoft.Test.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiImage {
        public static IEnumerable<AutomationElement> GetAll(AutomationElement window) {
            var res = window.FindAllChildrenByClassName("Image");
            return res;
        }

        public static GuiImage Find(AutomationElement window, string automationId) {
            var res = window.FindChildByClassAndAutomationId("Image", automationId);
            return new GuiImage(res);
        }


        public readonly AutomationElement img;

        public GuiImage(AutomationElement imgElement) {
            img = imgElement;
        }

        public void LeftClickMouse(int offSetHeightFromCenter = 0, bool leftEdge = false) {
            ClickMouse(offSetHeightFromCenter, MouseButton.Left, leftEdge);
        }

        public void RightClickMouse(int offSetHeightFromCenter = 0, bool leftEdge = false) {
            ClickMouse(offSetHeightFromCenter, MouseButton.Right, leftEdge);
        }

        private void ClickMouse(int offSetHeightFromCenter, MouseButton btn, bool leftEdge) {
            img.ClickPointFromCenter(btn, offSetHeightFromCenter, leftEdge);
        }

        public void IsNotShowing() {
            Assert.IsTrue(img.Current.BoundingRectangle.IsEmpty);
        }

        public void IsShowing() {
            Assert.IsFalse(img.Current.BoundingRectangle.IsEmpty);
        }
    }
}