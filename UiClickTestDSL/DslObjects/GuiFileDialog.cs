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
            int maxRetries = 120;
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
                Thread.Sleep(500);
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
            Thread.Sleep(500);
            if (ApplicationLauncher.VerifyOnTestMachine())
                Thread.Sleep(1500);
            UiTestDslCoreCommon.WaitWhileBusy();
            if (ApplicationLauncher.VerifyOnTestMachine())
                Thread.Sleep(5000);
        }

        public void Cancel() {
            //UiTestDslCoreCommon.WaitWhileBusy();
            //todo after upgradering to .Net 4.6: 
            UiTestDslCoreCommon.RepeatTryingFor(TimeSpan.FromMinutes(5), () => GuiTextBox.GetTextBoxByName(_dialog, "File name:"));
            UiTestDslCoreCommon.WaitWhileBusy();
            SendKeys.SendWait("{Esc}");
            Thread.Sleep(2000);
            UiTestDslCoreCommon.WaitWhileBusy();
            if (ApplicationLauncher.VerifyOnTestMachine())
                Thread.Sleep(5000);
        }
    }
}
