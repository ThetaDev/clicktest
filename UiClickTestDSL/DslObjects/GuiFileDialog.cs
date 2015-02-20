using System;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.DslObjects {
    public class GuiFileDialog {
        public static GuiFileDialog Find(AutomationElement window, string caption) {
            int maxRetries = UiTestDslCoreCommon.MaxConnectionRetries;
            AutomationElement res = null;
            while (window == null && maxRetries > 0) {
                try {
                    window = window.FindChildByLocalizedControlTypeAndName(caption, "Dialog");
                } catch (Exception) {
                    if (maxRetries > 0)
                        maxRetries--;
                    else
                        throw;
                }
                if (window == null)
                    Thread.Sleep(500);
                maxRetries--;
            }
            Thread.Sleep(500);
            return new GuiFileDialog(res, window, caption);
        }

        private AutomationElement dialog;
        private readonly AutomationElement parent;
        private readonly string caption;

        public GuiFileDialog(AutomationElement dialog, AutomationElement parent, string caption) {
            this.dialog = dialog;
            this.parent = parent;
            this.caption = caption;
        }

        public void SelectFile(string filePathAndName) {
            UiTestDslCoreCommon.WaitWhileBusy();
            Thread.Sleep(500);
            if (ApplicationLauncher.VerifyOnTestMachine())
                Thread.Sleep(6000);
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
            UiTestDslCoreCommon.WaitWhileBusy();
            Thread.Sleep(1000);
            if (ApplicationLauncher.VerifyOnTestMachine())
                Thread.Sleep(5000);
            UiTestDslCoreCommon.WaitWhileBusy();
            SendKeys.SendWait("{Esc}");
            Thread.Sleep(2000);
            UiTestDslCoreCommon.WaitWhileBusy();
            if (ApplicationLauncher.VerifyOnTestMachine())
                Thread.Sleep(5000);
        }
    }
}
