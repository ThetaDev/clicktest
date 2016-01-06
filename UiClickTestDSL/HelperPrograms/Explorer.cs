﻿using System;
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

        public ListUiItem GetFile(string filename) {
            try {
                //PrintAllControls(Window);
                var files = ListBox("Items View").GetAllUiItems();
                foreach (var f in files) {
                    Log.Debug("Found file: " + f.Name);
                }
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

        public void SelectFile(string file) {
            ListUiItem f = GetFile(file);
            f.SetFocus();
            Mouse.MoveTo(f.ClickablePoint);
            if (ApplicationLauncher.VerifyOnSingleClickMachine())
                Thread.Sleep(1000);
            else
                Mouse.Click(MouseButton.Left);
        }

        internal void DragDropFileTo(FileInfo file, AutomationElement el) {
            Start(file.DirectoryName);
            ListUiItem fileInExplorer;
            try {
                fileInExplorer = GetFile(file.Name);
            } catch (Exception) {
                Sleep(15);
                fileInExplorer = GetFile(file.Name);
            }
            Mouse.MoveTo(fileInExplorer.ClickablePoint);
            Mouse.Down(MouseButton.Left);
            Mouse.MoveTo(new Point(750, 500)); //trying to always move the cursor to an approximated center on a 1080p display, which is still within a 1366x768 display
            Minimize();
            Mouse.MoveTo(el.GetClickablePoint().Convert());
            Mouse.Up(MouseButton.Left);
        }

        internal void DragDropMultipleFilesTo(string[] files, string folder, AutomationElement el) {
            var dir = FileLocator.LocateFolder(folder);
            Start(dir.FullName);
            WaitWhileBusy();
            ListBox("Items View").TrySetFocus();
            try {
                SelectFile(files[0]);
            } catch (Exception) {
                Sleep(15);
                SelectFile(files[0]);
            }
            Keyboard.Press(Key.Ctrl);
            for (int i = 1; i < files.Count() - 1; i++) {
                SelectFile(files[i]);
            }
            var fileInExplorer = GetFile(files.Last());
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
