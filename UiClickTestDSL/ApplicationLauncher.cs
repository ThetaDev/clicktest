using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;
using UiClickTestDSL.HelperPrograms;

namespace UiClickTestDSL {
    public class ApplicationLauncher : IDisposable {
        /// <summary>
        /// The underlying process of the application
        /// </summary>
        public Process Process;
        public static bool ConnectedInsteadOfStarted { get; private set; }
        public bool RunApplicationClearUp = true;

        public static string ApplicationFolder;
        public static string ApplicationExeName;
        public static string Arguments;
        public static List<string> PossibleProcessNames = new List<string>();

        private static readonly List<string> ClickTestComputerNames = new List<string>();
        public static void AddNamesOfDedicatedClicktestComputers(params string[] names) {
            foreach (var name in names)
                ClickTestComputerNames.Add(name.ToUpper());
        }
        public static bool VerifyOnTestMachine() {
            string name = Environment.MachineName.ToUpper();
            return ClickTestComputerNames.Contains(name);
        }

        private static readonly List<string> SingleClickComputerNames = new List<string>();
        public static void AddNamesOfSingleClickComputers(params string[] names) {
            foreach (var name in names) {
                SingleClickComputerNames.Add(name);
            }
        }
        public static bool VerifyOnSingleClickMachine() {
            string name = Environment.MachineName.ToUpper();
            return SingleClickComputerNames.Contains(name);
        }

        public static Action CommonApplicationInit;
        public static Action ApplicationClearAfterTestRun;

        public List<string> FilesToDelete = new List<string> { @"C:\temp\test", @"C:\temp\test.zip" };
        public List<string> DirectoriesToDelete = new List<string> { @"C:\temp\test" };

        private static ILog Log = LogManager.GetLogger(typeof(ApplicationLauncher));

        public static IEnumerable<Process> FindProcess() {
            return PossibleProcessNames.FindProcess();
        }

        private void StartProcess() {
            string fileName = ApplicationFolder + Path.DirectorySeparatorChar + ApplicationExeName;
            if (!File.Exists(fileName))
                throw new ArgumentException("path doesn't exist: " + fileName);
            Process = new Process {
                StartInfo = {
                    FileName = fileName,
                    WorkingDirectory = ApplicationFolder,
                }
            };
            if (!string.IsNullOrWhiteSpace(Arguments))
                Process.StartInfo.Arguments = Arguments;
            Process.Start();
            Thread.Sleep(500);
        }

        public void LaunchApplication() {
            RunApplicationClearUp = true;
            ConnectedInsteadOfStarted = false;

            Process[] procs = PossibleProcessNames.FindProcess();
            if (procs.Length == 0) {
                try {
                    if (CommonApplicationInit != null)
                        CommonApplicationInit();
                    StartProcess();
                    Console.WriteLine("Started: " + Process.Id);
                    return;
                } catch (Exception) {
                    Close();
                    throw;
                }
            }
            if (procs.Length > 1)
                throw new Exception("Multiple windows found! Unable to continue");
            Process = procs[0];
            Console.WriteLine("Found: " + Process.Id);
            ConnectedInsteadOfStarted = true;
        }

        public void Close() {
            try {
                if (Process != null && !Process.HasExited) {
                    List<AutomationElement> dialogs = null;
                    string errorDialogHeading = "";
                    string screenShotFilename = "";
                    try {
                        var mainWindow = GetMainWindow();
                        dialogs = mainWindow.FindAllChildrenByByLocalizedControlType("Dialog").ToList();
                        foreach (var d in dialogs)
                            errorDialogHeading += d.Current.Name + "\n";
                        if (dialogs.Count > 0)
                            try {
                                screenShotFilename = ScreenShooter.SaveToFile();
                            } catch (Exception e) {
                                Log.Error("Exception while trying to save screenshot: " + e.Message, e);
                            }
                        if (!ConnectedInsteadOfStarted)
                            KillProcess();
                    } catch (AutomationElementNotFoundException) {
                        //I expect not to find these dialogs.
                    }
                    if (dialogs != null && dialogs.Count > 0)
                        Assert.AreEqual(0, dialogs.Count,
                            "Error dialog labeled \"" + errorDialogHeading +
                            "\" found when trying to close program. Screenshot: " + screenShotFilename);
                }
            } finally {
                if (!ConnectedInsteadOfStarted) {
                    KillProcess();
                }
                if (RunApplicationClearUp && ApplicationClearAfterTestRun != null)
                    ApplicationClearAfterTestRun();
                foreach (var file in FilesToDelete) {
                    if (File.Exists(file)) {
                        try {
                            File.Delete(file);
                        } catch {
                        }
                    }
                }
                foreach (var directory in DirectoriesToDelete) {
                    if (Directory.Exists(directory)) {
                        try {
                            Directory.Delete(directory, true);
                        } catch {
                        }
                    }
                }
            }
        }

        public void Dispose() {
            Close();
        }

        public static void KillAllOptionalProcesses() {
            try {
                HelperProgramSuper.TryKillHelperProcesses(PossibleProcessNames.ToArray());
            } catch (Exception ex) {
                Log.Error("Error trying to kill all primary processes: " + ex.Message, ex);
            }
        }

        private void KillProcess() {
            if (Process != null) {
                try {
                    if (!Process.HasExited) {
                        Process.Kill();
                    }
                    Process.WaitForExit(1000);
                } finally {
                    Process.Dispose();
                    // delete the reference
                    Process = null;
                }
            }
            Thread.Sleep(2000);
            KillAllOptionalProcesses();
            //Need to wait a bit to ensure the next test does not find this process just before it manages to exit
            Thread.Sleep(3000);
        }

        public void WaitForInputIdle() {
            if (Process.HasExited)
                return;
            Process.WaitForInputIdle();
        }

        public AutomationElement GetMainWindow() {
            Process.WaitForInputIdle();
            var res = AutomationElement.RootElement.FindChildByProcessId(Process.Id);
            return res;
        }

        public AutomationElement GetDialog(string caption) {
            var mainWindow = GetMainWindow();
            AutomationElement res = mainWindow.FindChildByLocalizedControlTypeAndName(caption, "Dialog", "dialog", "Window", "window", "Dialogboks", "dialogboks", "Vindu", "vindu");
            return res;
        }

        public void ShouldNotBeRunning() {
            Assert.IsTrue(Process.HasExited, "Program is still running");
        }

        public void IsAlive() {
            Assert.IsFalse(Process.HasExited, "Program has exited");
        }
    }
}

