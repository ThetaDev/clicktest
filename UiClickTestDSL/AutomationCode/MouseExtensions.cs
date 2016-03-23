using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using Microsoft.Test.Input;
using UiClickTestDSL.HelperPrograms;
using Point = System.Drawing.Point;

namespace UiClickTestDSL.AutomationCode {
    public static class MouseExtensions {
        public static Point Convert(this System.Windows.Point pnt) {
            return new Point((int)pnt.X, (int)pnt.Y);
        }

        public static void ClickPointInCenter(this AutomationElement el, MouseButton btn) {
            el.ClickPointFromCenter(btn, 0, false);
        }

        public static void ClickPointFromCenter(this AutomationElement el, MouseButton btn, int offSetHeightFromCenter, bool leftEdge) {
            el.MoveMouseToCenter(leftEdge, offSetHeightFromCenter);
            Mouse.Click(btn);
        }

        public static void MoveMouseToCenter(this AutomationElement el, bool leftEdge = false, int offSetHeightFromCenter = 0) {
            Rect bounds = el.Current.BoundingRectangle;

            int centerX = leftEdge ? (int)bounds.X : (int)(bounds.X + bounds.Width / 2);
            int centerY = (int)(bounds.Y + bounds.Height / 2) + offSetHeightFromCenter;
            if (centerX == 0 && centerY == 0) {
                //this is most likely because we are waiting for the image to start showing.
                Thread.Sleep(5000);
                centerX = leftEdge ? (int)bounds.X : (int)(bounds.X + bounds.Width / 2);
                centerY = (int)(bounds.Y + bounds.Height / 2) + offSetHeightFromCenter;
            }
            //Console.WriteLine("X: " + centerX + " Y: " + centerY);
            Mouse.MoveTo(new Point(centerX, centerY));
        }

        public static void ClickPointInCenterHeight(this AutomationElement el, MouseButton btn, int offSetWidthFromLeft = 10) {
            Rect bounds = el.Current.BoundingRectangle;

            int centerX = (int)bounds.X + offSetWidthFromLeft;
            int centerY = (int)(bounds.Y + bounds.Height / 2);
            //Console.WriteLine("X: " + centerX + " Y: " + centerY);
            Mouse.MoveTo(new Point(centerX, centerY));
            Mouse.Click(btn);
        }

        public static void ClickPointInCenterHeightPercentageWidth(this AutomationElement el, MouseButton btn, int percentageOfWidth) {
            Rect bounds = el.Current.BoundingRectangle;

            int centerX = (int)(bounds.X + (bounds.Width * percentageOfWidth / 100));
            int centerY = (int)(bounds.Y + bounds.Height / 2);
            //Console.WriteLine("X: " + centerX + " Y: " + centerY);
            Mouse.MoveTo(new Point(centerX, centerY));
            Mouse.Click(btn);
        }

        public static void DragAndDropFileFromExplorerToCenter(this AutomationElement el, FileInfo file, Explorer preStartedExplorer = null) {
            var explorer = preStartedExplorer ?? new Explorer("TestFiles");
            try {
                explorer.DragDropFileTo(file, el);
            } finally {
                Thread.Sleep(1000);
                explorer.Dispose();
            }
        }

        public static void DragAndDropMultipleFilesFromExplorerToCenter(this AutomationElement el, string folder, Explorer preStartedExplorer, params string[] files) {
            if (files.Count() < 2)
                throw new Exception("Must have at least two files");
            var explorer = preStartedExplorer ?? new Explorer("TestFiles");
            try {
                explorer.DragDropMultipleFilesTo(files, folder, el);
            } finally {
                Thread.Sleep(1000);
                explorer.Dispose();
            }
        }
    }
}
