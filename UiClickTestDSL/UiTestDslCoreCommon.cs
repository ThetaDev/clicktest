using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using log4net;
using log4net.Config;
using Microsoft.Test.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using UiClickTestDSL.DslObjects;

namespace UiClickTestDSL {
    [TestClass]
    public abstract class UiTestDslCoreCommon {
        public static readonly string AssemblyDir = String.Format(@"{0}\", Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6));
        private static ILog Log = LogManager.GetLogger(typeof(UiTestDslCoreCommon));

        static UiTestDslCoreCommon() {
            var configfile = new FileInfo(AssemblyDir + @"\log4net.config");
            if (!configfile.Exists)
                throw new FileNotFoundException("Log4net config file not found!");
            XmlConfigurator.Configure(configfile);
        }

        protected static ApplicationLauncher Program = new ApplicationLauncher();
        public AutomationElement Window;
        public const int MaxConnectionRetries = 120;

        public void DontRunCleanupOnRestart() {
            Program.RunApplicationClearUp = false;
        }

        public static string UniqueIdentifier = null;
        public static string shortUnique = null;
        public string CreateNewUniqueIdentifier() {
            UniqueIdentifier = Guid.NewGuid().ToString();
            shortUnique = UniqueIdentifier.Replace("-", "").Remove(14);
            Print(UniqueIdentifier);
            return UniqueIdentifier;
        }

        public static void WaitWhileBusy() {
            Sleep(1);
            Program.WaitForInputIdle();
            Sleep(1);
        }

        public static void RepeatTryingFor(TimeSpan time, Action todo, int sleepInterval = 1000) {
            var sw = Stopwatch.StartNew();
            bool waiting = true;
            Exception lastException = null;
            while (waiting && sw.ElapsedMilliseconds < time.TotalMilliseconds) {
                try {
                    todo();
                    lastException = null;
                    waiting = false;
                } catch (Exception ex) {
                    lastException = ex;
                }
                Thread.Sleep(sleepInterval);
            }
            sw.Stop();
            if (lastException != null)
                throw lastException;
        }

        [TestCleanup]
        public virtual void CloseApplication() {
            Program.Close();
            InvalidateCachedObjects();
        }

        public virtual void GetThisWindow() {
            int maxRetries = MaxConnectionRetries;
            Window = null;
            HashSet<string> exceptions = new HashSet<string>();
            while (Window == null && maxRetries > 0) {
                try {
                    Window = Program.GetMainWindow();
                } catch (Exception ex) {
                    exceptions.Add(ex.Message);
                    if (maxRetries > 0)
                        maxRetries--;
                    else
                        throw;
                }
                if (Window == null)
                    Sleep(1);
                maxRetries--;
            }
            Log.Debug($"Found window after {MaxConnectionRetries-maxRetries} retries. The following exceptions were encountered:");
            foreach (var msg in exceptions) {
                Log.Debug(msg);
            }
            WaitWhileBusy();
        }

        public static void Sleep(int seconds, bool actuallyMoreThanOneMinuteSleep = false) {
            if (seconds >= 60 && !actuallyMoreThanOneMinuteSleep) {
                Assert.Fail($"Was told to wait more than one minute ({seconds}s), but no extra confirmation given.");
            }
            seconds = Math.Max(1, seconds);
            Thread.Sleep(seconds * 1000);
        }

        public static void SleepMilliseconds(int milliSeconds) {
            Thread.Sleep(milliSeconds);
        }

        public static void SleepIfOnTestMachine(int seconds) {
            if (ApplicationLauncher.VerifyOnTestMachine())
                Sleep(seconds);
        }

        public virtual void MoveMouseHere() {
            Mouse.MoveTo(ClickablePoint);
        }

        public virtual void SingleClick(MouseButton button) {
            MoveMouseHere();
            Mouse.Click(button);
        }

        public virtual void DoubleClick(MouseButton button) {
            MoveMouseHere();
            Mouse.DoubleClick(button);
        }

        public virtual Point ClickablePoint {
            get { return Window.GetClickablePoint().Convert(); }
        }

        public virtual void PrintAllControls() {
            PrintAllControls(Window);
        }

        public static void PrintAllControls(AutomationElement ae) {
            PrintLine("All controls:");
            var all = ae.FindAll(TreeScope.Subtree, Condition.TrueCondition); // .Descendants
            PrintAutomationElements(all);
        }

        private static void PrintAutomationElements(AutomationElementCollection elements) {
            foreach (var c in elements) {
                try {
                    var ae = (c as AutomationElement);
                    PrintLine(PadToLength(ae.Current.ClassName) + " " + PadToLength(ae.Current.AutomationId) + " " + ae.Current.Name);
                } catch (Exception) { }
            }
        }

        public virtual void PrintControls() {
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
            Log.Debug(text);
            Console.Write(text);
        }

        public static void PrintLine(string text = "") {
            Log.Debug(text);
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
            SendKeys.Flush();
            Sleep(2); //workaround for timing issues on different operating systems
        }

        public virtual string FindFileInAnyParentFolder(string filename) {
            return FileLocator.LocateFileInfo(filename).FullName;
        }

        public virtual string FindFolderInAnyParentFolder(string filename) {
            return FileLocator.LocateFolder(filename).FullName;
        }

        protected virtual void InvalidateCachedObjects() {
            GuiDataGrid.InvalidateCache();
            GuiTextBox.InvalidateCache();
            GuiDialog.InvalidateCache();
            GuiTabItem.InvalidateCache();
        }

        protected virtual void VerifyFileNotEmpty(string filenameAndPath) {
            var file = new FileInfo(filenameAndPath);
            Assert.AreNotEqual(0, file.Length);
        }

        protected virtual DateTime GetLastWriteTimeOfFile(string filnameAndPath) {
            var file = new FileInfo(filnameAndPath);
            return file.LastWriteTime;
        }

        protected virtual void VerifyFileHasGottenSuffix(string filenameAndPath, string fileSuffix) {
            Assert.IsFalse(File.Exists(filenameAndPath), "File should not have been found: " + filenameAndPath);
            Assert.IsTrue(File.Exists(filenameAndPath + fileSuffix), "Did not find file with correct suffix: " + filenameAndPath + " suffix: " + fileSuffix);
        }

        protected int RetriesToWaitForDialogToShow = 4;

        protected virtual void WaitUntilDialogIsShowing(string caption) {
            WaitWhileBusy();
            bool isWaiting = true;
            int i = RetriesToWaitForDialogToShow;
            while (isWaiting) {
                try {
                    Dialog(caption);
                    isWaiting = false;
                } catch (Exception) {
                    i--;
                    if (i == 0)
                        throw new Exception("Dialog with caption: \"" + caption + "\" never found");
                    Sleep(1);
                }
            }
            WaitWhileBusy();
        }

        public virtual GuiContextMenu ActiveContextMenu { get { return GuiContextMenu.GetActive(Window); } }
        public virtual GuiButton AppCloseButton { get { return GuiButton.GetAppCloseButton(Window); } }
        public virtual GuiExpander ExpanderByAutomationId(string automationId) { return GuiExpander.GetExpanderByAutomationId(Window, automationId); }
        public virtual GuiExpander Expander(ByAutomationId automationId) { return GuiExpander.GetExpanderByAutomationId(Window, automationId.Value); }
        public virtual GuiExpander Expander(string caption) { return GuiExpander.GetExpander(Window, caption); }

        protected virtual GuiDialog Dialog(string caption, bool quickCheck = false) { return GuiDialog.GetDialog(Program, Window, caption, quickCheck); }
        protected virtual GuiFileDialog OpenFileDialog(string caption) { return GuiFileDialog.Find(Window, caption); }

        public virtual void PrintTextBoxes() { PrintControls(GuiTextBox.GetAll(Window)); }
        public virtual GuiTextBox TextBox(string automationId) { return GuiTextBox.GetTextBox(Window, automationId); }
        public virtual GuiTextBox TextBox(ByAutomationId automationId) { return GuiTextBox.GetTextBox(Window, automationId.Value); }
        public virtual GuiTextBoxes TextBoxes(string prefix = "") { return GuiTextBoxes.GetAll(Window, prefix); }

        public virtual void PrintLabels(string prefix = "") { PrintControls(GuiLabel.GetAll(Window, prefix)); }
        public virtual GuiLabel Label(string automationId) { return GuiLabel.GetLabel(Window, automationId); }
        public virtual GuiLabel Label(ByAutomationId automationId) { return GuiLabel.GetLabel(Window, automationId.Value); }
        public virtual GuiLabels GetLabels(string prefix) { return GuiLabels.GetAll(Window, prefix); }

        public virtual void PrintButtons() { PrintControls(GuiButton.GetAll(Window)); }
        public virtual GuiButton ButtonByAutomationId(string automationId) { return GuiButton.GetButtonByAutomationId(Window, automationId); }
        public virtual GuiButton Button(ByAutomationId automationId) { return GuiButton.GetButtonByAutomationId(Window, automationId.Value); }
        public virtual GuiButton Button(string caption) { return GuiButton.GetButton(Window, caption); }

        public virtual GuiToggleButton ToggleButton(string automationId) { return GuiToggleButton.GetButtonByAutomationId(Window, automationId); }
        public virtual GuiToggleButton ToggleButton(ByAutomationId automationId) { return GuiToggleButton.GetButtonByAutomationId(Window, automationId.Value); }

        public virtual GuiRadioButton RadioButton(string caption) { return GuiRadioButton.GetRadioButton(Window, caption); }
        public virtual GuiRadioButton RadioButtonByAutomationId(string automationId) { return GuiRadioButton.GetRadioButtonByAutomationId(Window, automationId); }
        public virtual void PrintCheckBoxes() { PrintControls(GuiCheckBox.GetAll(Window)); }
        public virtual IEnumerable<GuiCheckBox> CheckBoxes() { return GuiCheckBox.GetAllCheckBoxes(Window); }
        public virtual GuiCheckBox CheckBox(string caption) { return GuiCheckBox.Find(Window, caption); }
        public virtual GuiCheckBox CheckBox(ByAutomationId automationId) { return GuiCheckBox.Find(Window, automationId); }

        public virtual void PrintDataGrids() { PrintControls(GuiDataGrid.GetAll(Window)); }
        public virtual GuiDataGrid DataGrid(string automationId) { return GuiDataGrid.GetDataGrid(Window, automationId); }
        public virtual GuiDataGrid DataGrid(ByAutomationId automationId) { return GuiDataGrid.GetDataGrid(Window, automationId.Value); }

        public virtual void PrintComboBoxes() { PrintControls(GuiComboBox.GetAll(Window)); }
        public virtual GuiComboBox ComboBox(string automationId) { return GuiComboBox.Find(Window, automationId); }
        public virtual GuiComboBox ComboBox(ByAutomationId automationId) { return GuiComboBox.Find(Window, automationId.Value); }
        public virtual GuiComboBoxes ComboBoxes(string prefix) { return GuiComboBoxes.Find(Window, prefix); }

        public virtual void PrintTabs() { PrintControls(GuiTabItem.GetAll(Window)); }
        public virtual GuiTabItem Tab(string name) { return GuiTabItem.GetTabByName(Window, name); }
        public virtual GuiTabItem Tab(ByAutomationId automationId) { return GuiTabItem.GetTabByAutomationId(Window, automationId.Value); }

        public virtual GuiImage Image(string automationId) { return GuiImage.Find(Window, automationId); }
        public virtual GuiImage Image(ByAutomationId automationId) { return GuiImage.Find(Window, automationId.Value); }

        public virtual GuiMenuItem Menu(string name) { return GuiMenuItem.GetMenuItem(Window, name); }
        public virtual GuiMenuItem Menu(ByAutomationId automationId) { return GuiMenuItem.GetMenuItemByAutomationId(Window, automationId.Value); }
        public virtual GuiMenuItem FirstMenuItem() { return GuiMenuItem.GetFirstMenuItem(Window); }

        public virtual GuiUserControl UserControl(string name) { return GuiUserControl.GetUserControl(Window, name); }

        public virtual GuiListBox ListBox(string name) { return GuiListBox.Find(Window, name); }
    }
}