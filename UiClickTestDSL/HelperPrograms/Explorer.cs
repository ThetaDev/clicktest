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
                           where f.Name.ToLower() == filename.ToLower()
                           select f;
                return item.First();
            } catch (Exception) {
                var screen = ScreenShooter.SaveToFile();
                Log.Debug("File not found; screenshot: " + screen);
                throw;
            }
        }

        public void SelectFile(List<ListUiItem> files, string file) {
            ListUiItem f = GetFile(files, file);
            f.SetFocus();
            Mouse.MoveTo(f.ClickablePoint);
            if (ApplicationLauncher.VerifyOnSingleClickMachine())
                Thread.Sleep(1000);
            else
                Mouse.Click(MouseButton.Left);
        }

        internal void DragDropFileTo(FileInfo file, AutomationElement el) {
            Start(file.DirectoryName);
            Maximize();
            WaitWhileBusy();
            ListBox("Items View").TrySetFocus();
            List<ListUiItem> files = GetAllFiles();
            ListUiItem fileInExplorer = GetFile(files, file.Name);
            Mouse.MoveTo(fileInExplorer.ClickablePoint);
            Mouse.Down(MouseButton.Left);
            Mouse.MoveTo(new Point(750, 500)); //trying to always move the cursor to an approximated center on a 1080p display, which is still within a 1366x768 display
            Minimize();
            Mouse.MoveTo(el.GetClickablePoint().Convert());
            Mouse.Up(MouseButton.Left);
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
            for (int i = 1; i < filenames.Count() - 1; i++) {
                SelectFile(files, filenames[i]);
            }
            var fileInExplorer = GetFile(files, filenames.Last());
            Mouse.MoveTo(fileInExplorer.ClickablePoint);
            Mouse.Down(MouseButton.Left);
            Keyboard.Release(Key.Ctrl);
            Mouse.MoveTo(new Point(750, 500)); //trying to always move the cursor to an approximated center on a 1080p display, which is still within a 1366x768 display

            Minimize();
            Mouse.MoveTo(el.GetClickablePoint().Convert());
            Mouse.Up(MouseButton.Left);
        }
    }
}
