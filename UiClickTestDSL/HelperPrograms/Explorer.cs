using System.IO;
using System.Linq;
using System.Windows.Automation;
using Microsoft.Test.Input;
using UiClickTestDSL.AutomationCode;
using UiClickTestDSL.DslObjects;

namespace UiClickTestDSL.HelperPrograms {
    public class Explorer : HelperProgramSuper {
        public Explorer(params string[] possibleProcesNames) {
            PossibleProcessNames.AddRange(possibleProcesNames);
        }

        protected override string ApplictionCommand {
            get { return "explorer"; }
        }

        public ListUiItem GetFile(string filename) {
            PrintAllControls(Window);
            var files = ListBox("Items View").GetAllUiItems();
            var item = from f in files
                       where f.Name.ToLower() == filename.ToLower()
                       select f;
            return item.First();
        }

        public void ClickFile(string file) {
            Mouse.MoveTo(GetFile(file).ClickablePoint);
            Mouse.Click(MouseButton.Left);
        }

        internal void DragDropFileTo(FileInfo file, AutomationElement el) {
            Start(file.DirectoryName);
            var fileInExplorer = GetFile(file.Name);
            Mouse.MoveTo(fileInExplorer.ClickablePoint);
            Mouse.Down(MouseButton.Left);
            Minimize();
            Mouse.MoveTo(el.GetClickablePoint().Convert());
            Mouse.Up(MouseButton.Left);
        }

        internal void DragDropMultipleFilesTo(string[] files, string folder, AutomationElement el) {
            var dir = FileLocator.LocateFolder(folder);
            Start(dir.FullName);
            ClickFile(files[0]);
            Keyboard.Press(Key.Ctrl);
            for (int i = 1; i < files.Count() - 1; i++) {
                ClickFile(files[i]);
            }
            var fileInExplorer = GetFile(files.Last());
            Mouse.MoveTo(fileInExplorer.ClickablePoint);
            Mouse.Down(MouseButton.Left);
            Keyboard.Release(Key.Ctrl);

            Minimize();
            Mouse.MoveTo(el.GetClickablePoint().Convert());
            Mouse.Up(MouseButton.Left);
        }
    }
}
