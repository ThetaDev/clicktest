using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UiClickTestDSL {
    public static class ScreenShooter {
        public static string ScreenShotFolder = @"C:\Temp\";

        private static string GetNextScreenShotFilename() {
            if (Directory.Exists(ScreenShotFolder)) {
                if (!ScreenShotFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    ScreenShotFolder += Path.DirectorySeparatorChar;
                var dir = new DirectoryInfo(ScreenShotFolder);
                if (!dir.Exists)
                    dir.Create();
                var names = (from f in dir.GetFiles()
                             where !f.Name.ToLower().Contains("thumbs")
                             select f.Name.Substring(0, f.Name.Length - 4));
                int test;
                var lastName = (from n in names
                                where Int32.TryParse(n, out test)
                                orderby Int32.Parse(n) descending
                                select n).FirstOrDefault();
                if (lastName == null)
                    return ScreenShotFolder + "1.jpg";
                Console.WriteLine("Last screenshot filename: " + lastName);
                int no = Int32.Parse(lastName);
                no++;
                return ScreenShotFolder + no + ".jpg";
            }
            return @"C:\Temp\ClickTestScreenShot.jpg";
        }

        public static string SaveToFile() {
            string filename = GetNextScreenShotFilename();
            var screenShotBmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);
            var screenGraphics = Graphics.FromImage(screenShotBmp);
            screenGraphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

            screenShotBmp.Save(filename, ImageFormat.Jpeg);
            screenGraphics.Dispose();
            screenShotBmp.Dispose();
            return filename;
        }
    }
}
