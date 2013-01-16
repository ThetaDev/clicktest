using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL.AutomationCode;

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

        public static Action CommonApplicationInit;
        public static Action ApplicationClearAfterTestRun;

        public List<string> FilesToDelete = new List<string> { @"C:\temp\test", @"C:\temp\test.zip" };

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

        public static bool VerifyOnTestMachine() {
            string name = Environment.MachineName.ToUpper();
            return ClickTestComputerNames.Contains(name);
        }

        public void Close() {
            if (Process != null) {
                List<AutomationElement> dialogs = null;
                string errorDialogHeading = "";
                string screenShotFilename = "";
                try {
                    var mainWindow = GetMainWindow();
                    dialogs = mainWindow.FindAllChildrenByByLocalizedControlType("Dialog").ToList();
                    foreach (var d in dialogs)
                        errorDialogHeading += d.Current.Name + "\n";
                    if (dialogs.Count > 0)
                        screenShotFilename = ScreenShooter.SaveToFile();
                    if (!ConnectedInsteadOfStarted)
                        KillProcess();
                } catch (AutomationElementNotFoundException) {
                    //I expect not to find these dialogs.
                }
                if (dialogs != null && dialogs.Count > 0)
                    Assert.AreEqual(0, dialogs.Count, "Error dialog labeled \"" + errorDialogHeading + "\" found when trying to close program. Screenshot: " + screenShotFilename);
            }
            if (!ConnectedInsteadOfStarted) {
                KillProcess();
            }
            if (RunApplicationClearUp && ApplicationClearAfterTestRun != null)
                ApplicationClearAfterTestRun();
            foreach (var file in FilesToDelete) {
                if (File.Exists(file)) {
                    try {
                        File.Delete(file);
                    } catch (Exception) { }
                }
            }
        }

        public void Dispose() {
            Close();
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
            AutomationElement res = mainWindow.FindChildByLocalizedControlTypeAndName(caption, "Dialog", "dialog", "Window", "window");
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

