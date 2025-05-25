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
using System.Linq;

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
            SleepMilliseconds(400);
            Program.WaitForInputIdle();
            SleepMilliseconds(400);
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
                throw new Exception($"Failed to do task, even when repeating for {time}.", lastException);
        }

        [TestCleanup]
        public virtual void CloseApplication() {
            InvalidateCachedObjects(); //Ensure no cached elements are found when checking for errors while closing the program
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
            Log.Debug($"Found window after {MaxConnectionRetries - maxRetries} retries. The following exceptions were encountered:");
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

        public virtual void DoubleClick(MouseButton button = MouseButton.Left) {
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
            PrintLine(PadToLength("Classname") + " " + PadToLength("AutomationId") + PadToLength("Name"));
            foreach (var c in elements) {
                try {
                    var ae = (c as AutomationElement);
                    PrintLine(PadToLength(PadToLength(ae.Current.ClassName) + " " + PadToLength(ae.Current.AutomationId) + " " + PadToLength(ae.Current.Name)));
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
            PrintLine(PadToLength("Classname") + " " + PadToLength("AutomationId") + PadToLength("Name"));
            foreach (var ae in controls) {
                PrintLine(PadToLength(PadToLength(ae.Current.ClassName)  + " " + PadToLength(ae.Current.AutomationId) + " " + PadToLength(ae.Current.Name)));
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
                    DialogC(caption);
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

        public void VisibleTextContains(params string[] texts) {
            string visibleText = GetVisibleText();
            foreach (var text in texts) {
                Assert.IsTrue(visibleText.Contains(text), "Visible text did not contain: " + text);
            }
        }

        public void VerifyVisibleTextContains(params string[] texts) {
            string visibleText = GetVisibleText();
            foreach (var text in texts) {
                Assert.Contains(text, visibleText);
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

        public virtual GuiContextMenu ActiveContextMenu { get { return GuiContextMenu.GetActive(Window); } }
        public virtual GuiButton AppCloseButton { get { return GuiButton.GetAppCloseButton(Window); } }
        public virtual GuiExpander ExpanderByAutomationId(string automationId) { return GuiExpander.GetExpanderByAutomationId(Window, automationId); }
        public virtual GuiExpander Expander(ByAutomationId automationId) { return GuiExpander.GetExpanderByAutomationId(Window, automationId.Value); }
        public virtual GuiExpander Expander(string caption) { return GuiExpander.GetExpander(Window, caption); }

        protected virtual GuiDialog DialogC(string caption, bool quickCheck = false, bool skipNetworkWait = false) { return GuiDialog.GetDialogC(Program, Window, caption, quickCheck, skipNetworkWait); }
        protected virtual GuiFileDialog OpenFileDialog(string caption) { return GuiFileDialog.Find(Window, caption); }

        public virtual void PrintTextBoxes() { PrintControls(GuiTextBox.GetAll(Window)); }
        public virtual GuiTextBox TextBoxC(string automationId) { return GuiTextBox.GetTextBoxC(Window, automationId); }
        public virtual GuiTextBox TextBoxByNameC(string name) { return GuiTextBox.GetTextBoxByNameC(Window, name); }
        public virtual GuiTextBox TextBoxC(ByAutomationId automationId) { return GuiTextBox.GetTextBoxC(Window, automationId.Value); }
        public virtual GuiTextBoxes TextBoxes(string prefix = "") { return GuiTextBoxes.GetAll(Window, prefix); }

        public virtual void PrintLabels(string prefix = "") { PrintControls(GuiLabel.GetAll(Window, prefix)); }
        public virtual GuiLabel Label(string automationId) { return GuiLabel.GetLabel(Window, automationId); }
        public virtual GuiLabel Label(ByAutomationId automationId) { return GuiLabel.GetLabel(Window, automationId.Value); }
        public virtual GuiLabel LabelByName(string name) { return GuiLabel.GetLabelByName(Window, name); }
        public virtual GuiLabels GetLabels(string prefix) { return GuiLabels.GetAll(Window, prefix); }

        public virtual void PrintButtons() { PrintControls(GuiButton.GetAll(Window)); }
        public virtual GuiButton ButtonByAutomationId(string automationId) { return GuiButton.GetButtonByAutomationId(Window, automationId); }
        public virtual GuiButton Button(ByAutomationId automationId) { return GuiButton.GetButtonByAutomationId(Window, automationId.Value); }
        public virtual GuiButton Button(string caption) { return GuiButton.GetButton(Window, caption); }
        public virtual GuiToggleButton ToggleButtonByName(string name) { return GuiToggleButton.GetButtonByName(Window, name); }

        public virtual GuiToggleButton ToggleButton(string automationId) { return GuiToggleButton.GetButtonByAutomationId(Window, automationId); }
        public virtual GuiToggleButton ToggleButton(ByAutomationId automationId) { return GuiToggleButton.GetButtonByAutomationId(Window, automationId.Value); }

        public virtual GuiRadioButton RadioButton(string caption) { return GuiRadioButton.GetRadioButton(Window, caption); }
        public virtual GuiRadioButton RadioButtonByAutomationId(string automationId) { return GuiRadioButton.GetRadioButtonByAutomationId(Window, automationId); }
        public virtual void PrintCheckBoxes() { PrintControls(GuiCheckBox.GetAll(Window)); }
        public virtual IEnumerable<GuiCheckBox> CheckBoxes() { return GuiCheckBox.GetAllCheckBoxes(Window); }
        public virtual GuiCheckBox CheckBox(string caption) { return GuiCheckBox.Find(Window, caption); }
        public virtual GuiCheckBox CheckBox(ByAutomationId automationId) { return GuiCheckBox.Find(Window, automationId); }

        public virtual void PrintDataGrids() { PrintControls(GuiDataGrid.GetAll(Window)); }
        public virtual GuiDataGrid DataGridC(string automationId) { return GuiDataGrid.GetDataGridC(Window, automationId); }
        public virtual GuiDataGrid DataGridC(ByAutomationId automationId) { return GuiDataGrid.GetDataGridC(Window, automationId.Value); }

        public virtual void PrintComboBoxes() { PrintControls(GuiComboBox.GetAll(Window)); }
        public virtual GuiComboBox ComboBox(string automationId) { return GuiComboBox.Find(Window, automationId); }
        public virtual GuiComboBox ComboBoxByName(string name) { return GuiComboBox.FindByName(Window, name); }
        public virtual GuiComboBox ComboBox(ByAutomationId automationId) { return GuiComboBox.Find(Window, automationId.Value); }
        public virtual GuiComboBoxes ComboBoxes(string prefix) { return GuiComboBoxes.Find(Window, prefix); }

        public virtual void PrintTabs() { PrintControls(GuiTabItem.GetAll(Window)); }
        public virtual GuiTabItem TabC(string name) { return GuiTabItem.GetTabByNameC(Window, name); }
        public virtual GuiTabItem TabC(ByAutomationId automationId) { return GuiTabItem.GetTabByAutomationIdC(Window, automationId.Value); }

        public virtual GuiImage Image(string automationId) { return GuiImage.Find(Window, automationId); }
        public virtual GuiImage Image(ByAutomationId automationId) { return GuiImage.Find(Window, automationId.Value); }

        public virtual GuiMenuItem Menu(string name) { return GuiMenuItem.GetMenuItem(Window, name); }
        public virtual GuiMenuItem Menu(ByAutomationId automationId) { return GuiMenuItem.GetMenuItemByAutomationId(Window, automationId.Value); }
        public virtual GuiMenuItem FirstMenuItem() { return GuiMenuItem.GetFirstMenuItem(Window); }

        public virtual GuiUserControl UserControl(string name) { return GuiUserControl.GetUserControl(Window, name); }

        public virtual GuiListBox ListBox(string name) { return GuiListBox.Find(Window, name); }
    }
}