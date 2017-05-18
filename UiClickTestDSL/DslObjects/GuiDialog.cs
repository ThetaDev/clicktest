using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiDialog : UiTestDslCoreCommon {
        private static GuiDialog _cachedDialog = null;
        private static ApplicationLauncher _currentProgram = null;
        private static AutomationElement _currentParentWindow = null;

        public static GuiDialog GetDialog(ApplicationLauncher program, AutomationElement parentWindow, string caption) {
            //kan kanskje få til noge med: window.GetMessageBox() i hoved dsl-klassen
            if (_cachedDialog == null || _cachedDialog.Caption != caption) {
                var dialog = program.GetDialog(caption);
                _cachedDialog = new GuiDialog(dialog, caption);
                _currentProgram = program;
                _currentParentWindow = parentWindow;
            }
            return _cachedDialog;
        }

        public static void InvalidateCache() {
            _cachedDialog = null;
        }



        public string Caption { get; private set; }

        public GuiDialog(AutomationElement dialogWindow, string caption) {
            Window = dialogWindow;
            Caption = caption;
        }

        public void CloseDialog() {
            var windowPattern = Window.GetPattern<WindowPattern>(WindowPattern.Pattern);
            windowPattern.Close();
        }

        public override void GetThisWindow() {
            _cachedDialog = GetDialog(_currentProgram, _currentParentWindow, Caption);
        }

        public void VisibleTextContains(params string[] texts) {
            string visibleText = GetVisibleText();
            foreach (var text in texts) {
                Assert.IsTrue(visibleText.Contains(text), "Visible text did not contain: " + text);
            }
        }

        private string GetVisibleText() {
            IEnumerable<AutomationElement> labels = GuiLabel.GetAll(Window, "");
            IEnumerable<string> texts = from l in labels
                                        where !l.Current.IsOffscreen
                                        select l.Current.Name;
            return texts.Aggregate("", (current, t) => current + t);
        }

        public void VisibleTextDoesNotContain(params string[] texts) {
            string visibleText = GetVisibleText();
            foreach (var text in texts) {
                Assert.IsFalse(visibleText.Contains(text), "Visible text should not, but contained: " + text);
            }
            
        }

        public void VisibleTextBoxesContains(string text) {
            string visibleText = GetVisibleTextBoxText();
            Assert.IsTrue(visibleText.Contains(text));
        }
        public void VisibleTextBoxesDoesNotContain(string text) {
            string visibleText = GetVisibleTextBoxText();
            Assert.IsFalse(visibleText.Contains(text));
        }
        private string GetVisibleTextBoxText() {
            GuiTextBoxes textBoxes = TextBoxes();
            IEnumerable<string> texts = from tb in textBoxes
                                        where tb.Visible
                                        select tb.Text;
            return texts.Aggregate("", (current, t) => current + t);
        }
    }
}
