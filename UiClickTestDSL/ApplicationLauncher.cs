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
using UiClickTestDSL.DslObjects;
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

        private static readonly List<string> MachinesWhereExplorerUsesCheckBoxes = new List<string>();
        public static void AddNamesOfComputersWhereExplorerUsesCheckBoxes(params string[] names) {
            foreach (var name in names) {
                MachinesWhereExplorerUsesCheckBoxes.Add(name);
            }
        }
        public static bool VerifyExplorerUsesCheckBoxes() {
            string name = Environment.MachineName.ToUpper();
            return MachinesWhereExplorerUsesCheckBoxes.Contains(name);
        }

        private static readonly List<string> DeveloperComputerNames = new List<string>();
        public static void AddNamesOfDeveloperComputers(params string[] names) {
            foreach (var name in names)
                DeveloperComputerNames.Add(name.ToUpper());
        }
        public static bool VerifyOnDeveloperMachine() {
            string name = Environment.MachineName.ToUpper();
            return DeveloperComputerNames.Contains(name);
        }

        public static Action CommonApplicationInit;
        public static Action ApplicationClearAfterTestRun;

        public static readonly string[] FilesToDelete = { @"C:\temp\test", @"C:\temp\test.zip" };
        public static readonly string[] DirectoriesToDelete = { @"C:\temp\test" };

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
            UiTestDslCoreCommon.SleepMilliseconds(500);
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
                    try {
                        var mainWindow = GetMainWindow();
                        dialogs = mainWindow.FindAllChildrenByByLocalizedControlType(AutomationExtensions.DialogLocalizedControlNameOptions).ToList();
                    } catch (AutomationElementNotFoundException) {
                        //I expect not to find these dialogs.
                    } catch {
                        try {
                            Log.Error("Error finding Main window and its dialogs, screenshot: " + ScreenShooter.SaveToFile());
                        } catch (Exception ex) {
                            Log.Error("Exception while trying to save screenshot: " + ex.Message, ex);
                        }
                    }
                    int problemDialogCounter = 0;
                    if (dialogs != null) {
                        foreach (var d in dialogs) {
                            UiTestDslCoreCommon.Sleep(1);
                            WaitForInputIdle();
                            var errorDialogHeading = d.Current.Name;
                            if (string.IsNullOrEmpty(errorDialogHeading)) {
                                continue; //Some controls like ContextMenus show up as dialogs, even though they do not support the WindowPattern
                            }
                            problemDialogCounter++;

                            string screenShotFilename = string.Empty;
                            try {
                                screenShotFilename = ScreenShooter.SaveToFile();
                            } catch (Exception ex) {
                                Log.Error("Exception while trying to save screenshot: " + ex.Message, ex);
                            }
                            Log.Error(string.Format("Error dialog labeled \"{0}\" found when trying to close program. Screenshot: {1}", errorDialogHeading, screenShotFilename));
                            try {
                                var guiDialog = new GuiDialog(d, errorDialogHeading);
                                guiDialog.CloseDialog();
                            } catch (Exception ex) {
                                Log.Error(string.Format("Attempted to close dialog labeled \"{0}\", but an exception occurred.", errorDialogHeading), ex);
                            }
                        }
                        if (!ConnectedInsteadOfStarted)
                            KillProcess();
                        Assert.AreEqual(0, problemDialogCounter, "Error dialogs found when trying to close program.");
                    }
                }
            } finally {
                if (!ConnectedInsteadOfStarted) {
                    KillProcess();
                }
                if (RunApplicationClearUp && ApplicationClearAfterTestRun != null)
                    ApplicationClearAfterTestRun();
                CleanUpFilesAndDirectories();
            }
        }

        public static void CleanUpFilesAndDirectories() {
            foreach (var file in FilesToDelete) {
                if (File.Exists(file)) {
                    try {
                        File.Delete(file);
                    } catch (Exception ex) {
                        Log.Error(string.Format("Failed to delete file during cleanup. Filename: {0}", file), ex);
                    }
                }
            }
            foreach (var directory in DirectoriesToDelete) {
                if (Directory.Exists(directory)) {
                    try {
                        DirectoryHelper.DeleteDirectory(directory);
                    } catch (Exception ex) {
                        Log.Error(string.Format("Failed to delete directory during cleanup. Directory: {0}", directory), ex);
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
            UiTestDslCoreCommon.Sleep(2);
            KillAllOptionalProcesses();
            //Need to wait a bit to ensure the next test does not find this process just before it manages to exit
            UiTestDslCoreCommon.Sleep(3);
        }

        public void WaitForInputIdle() {
            if (Process == null || Process.HasExited)
                return;
            if (!Process.WaitForInputIdle(5000))
                Process.WaitForInputIdle(5000);
        }

        public AutomationElement GetMainWindow() {
            Process.WaitForInputIdle(5000);
            //if (_timer != null) Log.Info("GetMainWindow.WaitForInputIdle: "+_timer.ElapsedMilliseconds);
            var res = AutomationElement.RootElement.FindChildByProcessId(Process.Id);
            //if (_timer != null) Log.Info("GetMainWindow -> ActualSearch: " + _timer.ElapsedMilliseconds);
            return res;
        }

        //private Stopwatch _timer;

        public AutomationElement GetDialog(string caption, bool quickCheck) {
            //_timer = Stopwatch.StartNew();
            var mainWindow = GetMainWindow();
            AutomationElement res = mainWindow.FindChildByLocalizedControlTypeAndName(caption, quickCheck, AutomationExtensions.DialogLocalizedControlNameOptions);
            //_timer.Stop();
            //Log.Info("GetDialog -> ActualSearch: " + _timer.ElapsedMilliseconds);
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

