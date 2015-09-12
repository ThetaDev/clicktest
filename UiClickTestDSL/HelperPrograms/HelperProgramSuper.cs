using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using UiClickTestDSL.AutomationCode;

namespace UiClickTestDSL.HelperPrograms {
    public abstract class HelperProgramSuper : UiTestDslCoreCommon, IDisposable {
        private static ILog Log = LogManager.GetLogger(typeof(HelperProgramSuper));

        public Process Process;
        protected abstract string ApplictionCommand { get; }
        protected List<string> PossibleProcessNames = new List<string>();

        public static int TryKillHelperProcesses(params string[] possibleProcessNames) {
            Process[] procs = possibleProcessNames.FindProcess();
            foreach (var process in procs) {
                try {
                    process.Kill();
                } catch (Exception ex) { 
                    Log.Error("Error killing process: "+ex.Message, ex);
                }
            }
            return procs.Length;
        }

        public void Start(string arguments) {
            if (!PossibleProcessNames.Contains(ApplictionCommand))
                PossibleProcessNames.Add(ApplictionCommand);
            var pidsAlreadyStarted = from p in PossibleProcessNames.FindProcess()
                                     select p.Id;
            foreach (var i in pidsAlreadyStarted) {
                Console.Write(i + ", ");
            }
            Console.WriteLine();
            Process = new Process {
                StartInfo = {
                    FileName = ApplictionCommand,
                    Arguments = arguments,
                }
            };
            Process.Start();
            Thread.Sleep(500);
            if (!Process.HasExited)
                Process.WaitForInputIdle(60000);
            SleepIfOnTestMachine(30000);
            var findProcess = PossibleProcessNames.FindProcess();
            foreach (var p in findProcess)
                Console.WriteLine("Found process: " + p.Id + " " + p.ProcessName);
            Process = findProcess.First(p => !pidsAlreadyStarted.Contains(p.Id));
            Console.WriteLine("New process is: " + Process.Id + " " + Process.ProcessName);
            GetActualWindow();
        }

        public void GetActualWindow() {
            int maxRetries = MaxConnectionRetries;
            Window = null;
            while (Window == null && maxRetries > 0) {
                try {
                    Process.WaitForInputIdle();
                    Window = AutomationElement.RootElement.FindChildByProcessId(Process.Id);
                } catch {
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
            Window.SetFocus();
            WaitWhileBusy();
        }

        public override void GetThisWindow() {
            GetActualWindow();
        }

        private void KillProcess() {
            if (Process != null) {
                Console.WriteLine("Process " + Process.Id);
                try {
                    if (!Process.HasExited) {
                        Console.WriteLine("Killing process " + Process.Id);
                        Process.Kill();
                    }
                    Process.WaitForExit(1000); //trenger å vente litt her for å passe på at ikkje neste test som kjøres finner igjen samme process akkurat før den avslutter
                } finally {
                    Process.Dispose();
                    // delete the reference
                    Process = null;
                }
            }
        }

        public void Dispose() {
            KillProcess();
        }

        public void Minimize() {
            Process.Minimize();
        }
    }
}
