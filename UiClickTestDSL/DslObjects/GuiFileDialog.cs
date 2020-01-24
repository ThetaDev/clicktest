using System;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using log4net;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiFileDialog {
        private static ILog Log = LogManager.GetLogger(typeof(GuiFileDialog));

        public static GuiFileDialog Find(AutomationElement window, string caption) {
            AutomationElement dlg = null;
            //* Works fine on Windows 8.1, but not on Windows 7 with only .Net 4.0 installed.
            // * We need to retest this after upgrading to .Net 4.6
            int maxRetries = 300; //try for at least 3 minutes; Our virtual machines are sometimes slow when opening file type dialogs/explorer.
            while (dlg == null && maxRetries > 0) {
                try {
                    //dlg = window.FindChildByLocalizedControlTypeAndName(caption, AutomationExtensions.DialogLocalizedControlNameOptions); //different name options on different language settings
                    dlg = window.FindChildByControlTypeAndName(ControlType.Window, caption);
                } catch (Exception) {
                    if (maxRetries <= 0)
                        throw;
                }
                if (dlg == null && maxRetries % 10 == 0) {
                    Log.DebugFormat("File dialog not found: {0}   Caption: {1}", maxRetries, caption);
                }
                UiTestDslCoreCommon.SleepMilliseconds(500);
                maxRetries--;
            }
            //*/

            //Until this method gets reworked, let developer-machines sleep for 6 seconds instead of 60, for faster testing
            /*
            if (ApplicationLauncher.VerifyOnDeveloperMachine()) {
                Thread.Sleep(6000);
            } else {
                Thread.Sleep(60000);
            }
             */
            return new GuiFileDialog(dlg, window, caption);
        }

        private readonly AutomationElement _dialog;
        private readonly AutomationElement _parent;
        private readonly string _caption;

        public GuiFileDialog(AutomationElement dialog, AutomationElement parent, string caption) {
            _dialog = dialog;
            _parent = parent;
            _caption = caption;
        }

        public override string ToString() {
            return GetType().Name + ": " + _caption + "; " + _parent;
        }

        public void SelectFile(string filePathAndName) {
            //UiTestDslCoreCommon.WaitWhileBusy();
            //todo after upgradering to .Net 4.6: 
            UiTestDslCoreCommon.RepeatTryingFor(TimeSpan.FromMinutes(5), () => GuiTextBox.GetTextBoxByName(_dialog, "File name:"));
            UiTestDslCoreCommon.WaitWhileBusy();
            SendKeys.SendWait(filePathAndName);
            SendKeys.SendWait("{Enter}");
            //TODO: Is all this sleep required?
            UiTestDslCoreCommon.SleepMilliseconds(500);
            UiTestDslCoreCommon.SleepIfOnTestMachine(1);
            UiTestDslCoreCommon.WaitWhileBusy();
            UiTestDslCoreCommon.SleepIfOnTestMachine(5);
        }

        public void Cancel() {
            //UiTestDslCoreCommon.WaitWhileBusy();
            //todo after upgradering to .Net 4.6: 
            UiTestDslCoreCommon.RepeatTryingFor(TimeSpan.FromMinutes(5), () => GuiTextBox.GetTextBoxByName(_dialog, "File name:"));
            UiTestDslCoreCommon.WaitWhileBusy();
            SendKeys.SendWait("{Esc}");
            //TODO: Is all this sleep required?
            UiTestDslCoreCommon.Sleep(2);
            UiTestDslCoreCommon.WaitWhileBusy();
            UiTestDslCoreCommon.SleepIfOnTestMachine(5);
        }
    }
}
