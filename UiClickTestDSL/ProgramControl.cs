using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using log4net;

namespace UiClickTestDSL {
    public static class ProgramControl {
        private static ILog Log = LogManager.GetLogger(typeof(ProgramControl));

        public static Process[] FindProcess(this List<string> possibleProcessNames) {
            return possibleProcessNames.ToArray().FindProcess();
        }

        public static Process[] FindProcess(this string[] possibleProcessNames) {
            Process[] procs = null;
            int i = 0;
            while (procs == null || procs.Length == 0 || procs[0].PriorityClass == ProcessPriorityClass.Idle) {
                try {
                    procs = Process.GetProcessesByName(possibleProcessNames[i]);
                } catch (Exception e) {
                    Log.Error("Error getting processes with name: " + possibleProcessNames[i], e);
                    procs = null;
                }
                i++;
                if (i >= possibleProcessNames.Length)
                    break;
            }
            return procs;
        }

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void Minimize(this Process process) {
            ShowWindow(process.MainWindowHandle, 2);
        }

        public static void Maximize(this Process process) {
            ShowWindow(process.MainWindowHandle, 3);
        }
    }
}
