using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UiClickTestDSL {
    public static class ProgramControl {
        public static Process[] FindProcess(this List<string> possibleProcessNames) {
            Process[] procs = null;
            int i = 0;
            while (procs == null || procs.Length == 0 || procs[0].PriorityClass == ProcessPriorityClass.Idle) {
                procs = Process.GetProcessesByName(possibleProcessNames[i]);
                i++;
                if (i >= possibleProcessNames.Count)
                    break;
            }
            return procs;
        }

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void Minimize(this Process process) {
            ShowWindow(process.MainWindowHandle, 2);
        }
    }
}
