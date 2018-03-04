using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UiClickTestDSL;
using UiClickTestDSL.AutomationCode;

namespace DelfiCertUiTestDSL {
    [TestClass]
    class Program {
        private static ILog Log = LogManager.GetLogger(typeof(Program));

        private const bool DeleteOnDevelopersComputer = false;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context) {
            SetApplicationDetails();
            AddNameOptions();

            ApplicationLauncher.AddNamesOfDedicatedClicktestComputers("KLIKKTEST", "DOTSTDCERT7X64");

            ApplicationLauncher.CommonApplicationInit = () => {
                if (ApplicationLauncher.VerifyOnTestMachine()) {
                    File.Copy(@"..\DelfiCert.ini.Klikktest", @"..\DelfiCert.ini", true);
                    File.AppendAllText(@"..\Service\web.config", " ");
                    //this should restart the running web service
                    Thread.Sleep(2000); //Allow the web service to restart
                }
            };
            ApplicationLauncher.ApplicationClearAfterTestRun = () => {
                if (!ApplicationLauncher.ConnectedInsteadOfStarted && (ApplicationLauncher.VerifyOnTestMachine() || DeleteOnDevelopersComputer)) {
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    Console.WriteLine("Current dir:" + Directory.GetCurrentDirectory());
                    File.Copy(FileLocator.LocateFileInfo("Client.ini").FullName, dir + @"\DelfiCert\DelfiCertClient.ini", true);
                }
            };

            ScreenShooter.ScreenShotFolder = @"C:\DelfiCertTestFolder\ScreenShots\";
        }

        private static void AddNameOptions() {
            AutomationExtensions.AddNameOption("Add", "Select", "Select This Item");
            AutomationExtensions.AddNameOption("Cancel", "Clear");
            AutomationExtensions.AddNameOption("Close", "Clear");
            AutomationExtensions.AddNameOption("Delete Page", "_Delete Page");
            AutomationExtensions.AddNameOption("Undelete Page", "_Undelete Page", "Undelete");
            AutomationExtensions.AddNameOption("Edit Certificate", "Open Certificate Editor");
            AutomationExtensions.AddNameOption("Register", "Save", "Lagre");
        }

        private static void SetApplicationDetails() {
            ApplicationLauncher.ApplicationFolder = @"..\KlientGUI";
            ApplicationLauncher.ApplicationExeName = "DelfiCert.exe";
            ApplicationLauncher.PossibleProcessNames.Add("DelfiCert.vshost");
            ApplicationLauncher.PossibleProcessNames.Add("DelfiCert");
        }

        public static void ResetDatabase() {
            if (ApplicationLauncher.VerifyOnTestMachine()) {
                bool exited = false;
                try {
                    var proc = Process.Start(new ProcessStartInfo {
                        FileName = @"C:\DelfiCertTestFolder\4\SQL-scripts\relastKlikktestbase.cmd",
                        WorkingDirectory = @"C:\DelfiCertTestFolder\4\SQL-scripts",
                    });
                    exited = proc.WaitForExit(60000);
                } catch (Exception ex) {
                    Log.Error("Error reloading database: " + ex.Message, ex);
                }
                if (!exited)
                    throw new Exception("Timeout when waiting for database reloading.");
            }
        }

        static void Main(string[] args) {
            AssemblyInit(null);

            var runner = new MethodRunner(@"C:\DelfiCertTestFolder\Deployment\stopExecution.txt", @"C:\DelfiCertTestFolder\stopTesting");
            try {
                ResetDatabase();
                string filter = "";
                if (args.Length > 0)
                    filter = args[0].ToLower();
                runner.Run(Assembly.GetExecutingAssembly(), filter);
            } catch (Exception ex) {
                Log.Error("Top level error: " + ex.Message, ex);
                runner.ErrorCount++;
                if (ex.InnerException != null)
                    Log.Error("inner: " + ex.InnerException.Message + Helpers.ExctractAdditionalInformationFromException(ex.InnerException), ex.InnerException);
            }
            Environment.Exit(runner.ErrorCount);
        }
    }
}
