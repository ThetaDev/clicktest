using System;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Windows.Forms;
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
            UiTestDslCoreCommon.WaitWhileBusy();
        }

        public void IsNotShowing() {
            Assert.IsTrue(img.Current.BoundingRectangle.IsEmpty, "Image should not be showing");
        }

        public void IsShowing() {
            Assert.IsFalse(img.Current.BoundingRectangle.IsEmpty, "Image should be showing");
        }

        public void ShouldBeLandscape() {
            if (img.Current.BoundingRectangle.IsEmpty)
                throw new Exception("Image is not showing.");
            Assert.IsTrue(img.Current.BoundingRectangle.Width > img.Current.BoundingRectangle.Height);
        }

        public void ShouldBePortrait() {
            if (img.Current.BoundingRectangle.IsEmpty)
                throw new Exception("Image is not showing.");
            Assert.IsTrue(img.Current.BoundingRectangle.Width < img.Current.BoundingRectangle.Height);
        }

        public void ShouldBeSquare() {
            var rect = img.Current.BoundingRectangle;
            if (rect.IsEmpty)
                throw new Exception("Image is not showing.");
            int w = (int)rect.Width;
            int h = (int)rect.Height;
            var errorMsg = $"Actual size: {w}({rect.Width}) - {h}({rect.Height})";
            Assert.IsTrue(w == h, errorMsg);
        }
    }
}