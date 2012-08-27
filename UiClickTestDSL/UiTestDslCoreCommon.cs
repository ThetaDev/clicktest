using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using log4net.Config;
using Microsoft.Test.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using UiClickTestDSL.DslObjects;

namespace UiClickTestDSL {
    [TestClass]
    public abstract class UiTestDslCoreCommon {
        public static readonly string AssemblyDir = string.Format(@"{0}\", Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6));
        static UiTestDslCoreCommon() {
            var configfile = new FileInfo(AssemblyDir + @"\log4net.config");
            if (!configfile.Exists)
                throw new FileNotFoundException("Log4net config file not found!");
            XmlConfigurator.Configure(configfile);
        }

        protected static ApplicationLauncher Program = new ApplicationLauncher();
        protected AutomationElement Window;
        public const int MaxConnectionRetries = 120;

        public string UniqueIdentifier = null;
        public string CreateNewUniqueIdentifier() {
            UniqueIdentifier = Guid.NewGuid().ToString();
            Print(UniqueIdentifier);
            return UniqueIdentifier;
        }

        public static void WaitWhileBusy() {
            Sleep(500);
            Program.WaitForInputIdle();
            Sleep(500);
        }

        [TestCleanup]
        public void CloseApplicaiton() {
            Program.Close();
            InvalidateCachedObjects();
        }

        public virtual void GetThisWindow() {
            int maxRetries = MaxConnectionRetries;
            Window = null;
            while (Window == null && maxRetries > 0) {
                try {
                    Window = Program.GetMainWindow();
                } catch (Exception) {
                    if (maxRetries > 0)
                        maxRetries--;
                    else
                        throw;
                }
                if (Window == null)
                    Sleep(500);
                maxRetries--;
            }
            WaitWhileBusy();
        }

        public static void Sleep(int ms) {
            Thread.Sleep(ms);
        }

        public static void SleepIfOnTestMachine(int ms) {
            if (ApplicationLauncher.VerifyOnTestMachine())
                Sleep(ms);
        }

        public void MoveMouseHere() {
            var p = Window.GetClickablePoint();
            Mouse.MoveTo(new Point((int)p.X, (int)p.Y));
        }

        public void DoubleClick(MouseButton button) {
            MoveMouseHere();
            Mouse.DoubleClick(button);
        }

        public Point ClickablePoint {
            get { return Window.GetClickablePoint().Convert(); }
        }

        public void PrintAllControls() {
            PrintAllControls(Window);
        }

        public static void PrintAllControls(AutomationElement ae) {
            Console.WriteLine("All controls:");
            var all = ae.FindAll(TreeScope.Subtree, Condition.TrueCondition); // .Descendants
            PrintAutomationElements(all);
        }

        private static void PrintAutomationElements(AutomationElementCollection elements) {
            foreach (var c in elements) {
                try {
                    var ae = (c as AutomationElement);
                    Console.WriteLine(PadToLength(ae.Current.ClassName) + " " + PadToLength(ae.Current.AutomationId) + " " + ae.Current.Name);
                } catch (Exception) { }
            }
        }

        public void PrintControls() {
            PrintDataGrids();
            PrintTextBoxes();
            PrintButtons();
            PrintTabs();
            PrintLabels();
            PrintCheckBoxes();
            PrintComboBoxes();
        }

        internal static void PrintControls(IEnumerable<AutomationElement> controls) {
            foreach (var control in controls) {
                PrintLine(PadToLength(control.ToString()) + " | Name: " + PadToLength(control.Current.Name) + " | AutomationId: " + PadToLength(control.Current.AutomationId));
            }
        }

        private static string PadToLength(string text, int length = 30) {
            return text.PadRight(length, ' ');
        }

        public static void Print(string text) {
            Console.Write(text);
        }

        public static void PrintLine(string text = "") {
            Console.WriteLine(text);
        }

        public static void PressEnter() {
            SendKeys.SendWait("{Enter}");
            WaitWhileBusy();
        }

        /// <summary>
        ///  http://msdn.microsoft.com/en-us/library/system.windows.forms.sendkeys.aspx
        /// </summary>
        public static void PressKey(string keyName) {
            SendKeys.SendWait("{" + keyName + "}");
        }

        /// <summary>
        ///  http://msdn.microsoft.com/en-us/library/system.windows.forms.sendkeys.aspx
        /// </summary>
        public static void TypeText(string text) {
            SendKeys.SendWait(text);
        }

        public string FindFileInAnyParentFolder(string filename) {
            return FileLocator.LocateFileInfo(filename).FullName;
        }

        protected virtual void InvalidateCachedObjects() {
            GuiDataGrid.InvalidateCache();
            GuiTextBox.InvalidateCache();
            GuiDialog.InvalidateCache();
            GuiTabItem.InvalidateCache();
        }

        protected void VerifyFileNotEmpty(string filenameAndPath) {
            var file = new FileInfo(filenameAndPath);
            Assert.AreNotEqual(0, file.Length);
        }

        protected void VerifyFileHasGottenSuffix(string filenameAndPath, string fileSuffix) {
            Assert.IsFalse(File.Exists(filenameAndPath), "File should not have been found: " + filenameAndPath);
            Assert.IsTrue(File.Exists(filenameAndPath + fileSuffix), "Did not find file with correct suffix: " + filenameAndPath + " suffix: " + fileSuffix);
        }

        protected void WaitUntilDialogIsShowing(string caption) {
            WaitWhileBusy();
            bool isWaiting = true;
            int i = 30;
            while (isWaiting) {
                try {
                    Dialog(caption);
                    isWaiting = false;
                } catch (Exception) {
                    i--;
                    if (i == 0)
                        throw new Exception("Dialog with caption: \"" + caption + "\" never found");
                    Sleep(500);
                }
            }
            WaitWhileBusy();
        }

        public GuiContextMenu ActiveContextMenu { get { return GuiContextMenu.GetActive(Window); } }
        public GuiButton AppCloseButton { get { return GuiButton.GetAppCloseButton(Window); } }

        public GuiDialog Dialog(string caption) { return GuiDialog.GetDialog(Program, Window, caption); }
        public GuiFileDialog OpenFileDialog(string caption) { return GuiFileDialog.Find(Window, caption); }

        public void PrintTextBoxes() { PrintControls(GuiTextBox.GetAll(Window)); }
        public GuiTextBox TextBox(string automationId) { return GuiTextBox.GetTextBox(Window, automationId); }
        public GuiTextBoxes TextBoxes(string prefix = "") { return GuiTextBoxes.GetAll(Window, prefix); }

        public void PrintLabels(string prefix = "") { PrintControls(GuiLabel.GetAll(Window, prefix)); }
        public GuiLabel Label(string automationId) { return GuiLabel.GetLabel(Window, automationId); }
        public GuiLabels GetLabels(string prefix) { return GuiLabels.GetAll(Window, prefix); }

        public void PrintButtons() { PrintControls(GuiButton.GetAll(Window)); }
        public GuiButton ButtonByAutomationId(string automationId) { return GuiButton.GetButtonByAutomationId(Window, automationId); }
        public GuiButton Button(string caption) { return GuiButton.GetButton(Window, caption); }

        public GuiToggleButton ToggleButton(string automationId) { return GuiToggleButton.GetButtonByAutomationId(Window, automationId); }

        public GuiRadioButton RadioButton(string caption) { return GuiRadioButton.GetRadioButton(Window, caption); }
        public void PrintCheckBoxes() { PrintControls(GuiCheckBox.GetAll(Window)); }
        public GuiCheckBox CheckBox(string caption) { return GuiCheckBox.Find(Window, caption); }

        public void PrintDataGrids() { PrintControls(GuiDataGrid.GetAll(Window)); }
        public GuiDataGrid DataGrid(string automationId) { return GuiDataGrid.GetDataGrid(Window, automationId); }

        public void PrintComboBoxes() { PrintControls(GuiComboBox.GetAll(Window)); }
        public GuiComboBox ComboBox(string automationId) { return GuiComboBox.Find(Window, automationId); }
        public GuiComboBoxes ComboBoxes(string prefix) { return GuiComboBoxes.Find(Window, prefix); }

        public void PrintTabs() { PrintControls(GuiTabItem.GetAll(Window)); }
        public GuiTabItem Tab(string automationId) { return GuiTabItem.GetTab(Window, automationId); }

        public GuiImage Image(string automationId) { return GuiImage.Find(Window, automationId); }

        public GuiMenuItem Menu(string name) { return GuiMenuItem.GetMenuItem(Window, name); }
        public GuiMenuItem FirstMenuItem() { return GuiMenuItem.GetFirstMenuItem(Window); }

        public GuiUserControl UserControl(string name) { return GuiUserControl.GetUserControl(Window, name); }

        public GuiListBox ListBox(string name) { return GuiListBox.Find(Window, name); }
    }
}