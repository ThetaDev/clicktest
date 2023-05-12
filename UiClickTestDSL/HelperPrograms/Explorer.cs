using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using Microsoft.Test.Input;
using UiClickTestDSL.AutomationCode;
using UiClickTestDSL.DslObjects;
using log4net;

namespace UiClickTestDSL.HelperPrograms {
    public class Explorer : HelperProgramSuper {
        private static ILog Log = LogManager.GetLogger(typeof(Explorer));

        public Explorer(params string[] possibleProcesNames) {
            PossibleProcessNames.AddRange(possibleProcesNames);
        }

        protected override string ApplictionCommand {
            get { return "explorer"; }
        }

        private List<ListUiItem> GetAllFiles() {
            List<ListUiItem> files = null;
            RepeatTryingFor(TimeSpan.FromMinutes(3), () => files = ListBox("Items View").GetAllUiItems());
            if (files != null && files.Any()) {
                foreach (var f in files) {
                    Log.Debug("Found file: " + f.Name);
                }
            } else {
                var screen = ScreenShooter.SaveToFile();
                throw new Exception("No files not found; screenshot: " + screen);
            }
            return files;
        }

        public ListUiItem GetFile(List<ListUiItem> files, string filename) {
            try {
                //PrintAllControls(Window);
                var item = from f in files
                           where String.Equals(f.Name, filename, StringComparison.CurrentCultureIgnoreCase)
                           select f;
                return item.First();
            } catch (Exception) {
                var screen = ScreenShooter.SaveToFile();
                Log.Debug("File not found; screenshot: " + screen);
                throw;
            }
        }

        public ListUiItem SelectFile(List<ListUiItem> files, string file) {
            ListUiItem f = GetFile(files, file);
            f.SetFocus();
            Mouse.MoveTo(f.ClickablePoint);
            if (ApplicationLauncher.VerifyExplorerUsesCheckBoxes()) {
                f.AddToSelection();
            } else if (ApplicationLauncher.VerifyOnSingleClickMachine()) {
                Sleep(4);
            } else {
                Mouse.Click(MouseButton.Left);
            }
            Log.Debug("Selected file: " + file);
            return f;
        }

        internal void DragDropFileTo(FileInfo file, AutomationElement el) {
            Start(file.DirectoryName);
            Maximize();
            WaitWhileBusy();
            ListBox("Items View").TrySetFocus();
            List<ListUiItem> files = GetAllFiles();
            ListUiItem fileInExplorer = GetFile(files, file.Name);
            fileInExplorer.SetFocus();
            Mouse.MoveTo(new Point(fileInExplorer.ClickablePoint.X + 7, fileInExplorer.ClickablePoint.Y));
            Mouse.Down(MouseButton.Left);
            SleepMilliseconds(500);
            Mouse.MoveTo(new Point(1150, 600)); //ensure start of mouse drag
            SleepMilliseconds(500);
            Mouse.MoveTo(new Point(950, 500)); //trying to always move the cursor to the right of an approximated center on a 1080p display, which is still within a 1366x768 display
            SleepMilliseconds(500);
            Minimize();
            SleepMilliseconds(500);
            var centerElement = el.GetClickablePoint().Convert();
            Log.DebugFormat("Point to drag to x/y: {0}, {1}", centerElement.X, centerElement.Y);
            if (!(centerElement.X < 10 && centerElement.Y < 10)) //failsafe, for our tests it's better to keep the position of 750,500 than end up with 0,0
                Mouse.MoveTo(centerElement);
            SleepMilliseconds(500);
            Mouse.Up(MouseButton.Left);
            SleepMilliseconds(500);
        }

        internal void DragDropMultipleFilesTo(string[] filenames, string folder, AutomationElement el) {
            var dir = FileLocator.LocateFolder(folder);
            Start(dir.FullName);
            Maximize();
            WaitWhileBusy();
            ListBox("Items View").TrySetFocus();
            List<ListUiItem> files = GetAllFiles();
            SelectFile(files, filenames[0]);
            Keyboard.Press(Key.Ctrl);
            ListUiItem fileInExplorer = null;
            for (int i = 1; i < filenames.Count() - 1; i++) {
                fileInExplorer = SelectFile(files, filenames[i]);
            }
            Mouse.MoveTo(new Point(fileInExplorer.ClickablePoint.X + 7, fileInExplorer.ClickablePoint.Y));
            Mouse.Down(MouseButton.Left);
            Keyboard.Release(Key.Ctrl);
            SleepMilliseconds(500);
            Mouse.MoveTo(new Point(1150, 600)); //ensure start of mouse drag
            SleepMilliseconds(500);
            Mouse.MoveTo(new Point(950, 500)); //trying to always move the cursor to the right of an approximated center on a 1080p display, which is still within a 1366x768 display
            SleepMilliseconds(500);
            ScreenShooter.SaveToFile(); //todo fjern
            Minimize();
            SleepMilliseconds(500);
            var centerElement = el.GetClickablePoint().Convert();
            Log.DebugFormat("Point to drag to x/y: {0}, {1}", centerElement.X, centerElement.Y);
            if (!(centerElement.X == 0 && centerElement.Y == 0)) //failsafe, for our tests it's better to keep the position of 750,500 than end up with 0,0
                Mouse.MoveTo(centerElement);
            SleepMilliseconds(500);
            Mouse.Up(MouseButton.Left);
            SleepMilliseconds(500);
        }
    }
}
