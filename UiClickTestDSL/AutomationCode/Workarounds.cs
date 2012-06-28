using System;
using System.Threading;
using System.Windows.Automation;

namespace UiClickTestDSL.AutomationCode {
    public static class Workarounds {
        public static void TryUntilElementAvailable(Action action) {
            //workaround for our slow testcomputer
            int maxRetries = UiTestDslCoreCommon.MaxConnectionRetries;
            while (maxRetries > 0) {
                try {
                    action();
                    return;
                } catch (ElementNotAvailableException) {
                    if (maxRetries > 0)
                        maxRetries--;
                    else
                        throw;
                }
                Thread.Sleep(500);
                maxRetries--;
            }
        }

        public static T TryUntilElementAvailable<T>(Func<T> action) {
            //workaround for our slow testcomputer
            int maxRetries = UiTestDslCoreCommon.MaxConnectionRetries;
            while (maxRetries > 0) {
                try {
                    return action();
                } catch (ElementNotAvailableException) {
                    if (maxRetries > 0)
                        maxRetries--;
                    else
                        throw;
                }
                Thread.Sleep(500);
                maxRetries--;
            }
            return default(T);
        }
    }
}
