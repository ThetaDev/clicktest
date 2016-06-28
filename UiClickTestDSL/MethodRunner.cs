using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UiClickTestDSL {
    public class MethodRunner {
        static MethodRunner() {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
        }
        private static ILog Log = LogManager.GetLogger(typeof(MethodRunner));

        public int ErrorCount { get; set; }
        private readonly List<string> _filenamesThatStopTheTestRun;
        public string _settingsFilePath;
        public string _sectionedResultFilePath;

        public MethodRunner(params string[] filenamesThatStopTheTestRun) {
            emptyParams = new object[] { };
            _filenamesThatStopTheTestRun = new List<string>(filenamesThatStopTheTestRun);
        }

        public Func<string, bool> TestNameFilterHook = null;
        public Action<string> ErrorHook = null;
        public Action ResetTestEnvironment = null;
        private object[] emptyParams;

        private bool FilterByUserHook(string completeTestname) {
            if (TestNameFilterHook != null)
                return TestNameFilterHook(completeTestname);
            return false;
        }

        private List<TestDef> GetAllTests(Assembly testAssembly) {
            Type[] classes = testAssembly.GetTypes();
            List<TestDef> tests = new List<TestDef>();
            int i = 1;
            foreach (Type testclass in classes) {
                if (!testclass.IsDefined(typeof(TestClassAttribute), false))
                    continue;
                MethodInfo[] methods = testclass.GetMethods();
                foreach (MethodInfo testmethod in methods) {
                    if (testmethod.IsDefined(typeof(TestMethodAttribute), true)) {
                        tests.Add(new TestDef { TestClass = testclass, Test = testmethod, i = i++ });
                    }
                }
            }
            return tests;
        }

        private void LogNoConstructorError(string name) {
            Log.Error("--------------------------------------------------------------------------------------------------------");
            Log.Error("--------------------------------------------------------------------------------------------------------");
            Log.Error("Error: No constructor found for testclass: " + name);
            Log.Error("--------------------------------------------------------------------------------------------------------");
            Log.Error("--------------------------------------------------------------------------------------------------------");
            ErrorCount += 666;
        }

        public void Run(Assembly testAssembly, string filter) {
            int start = -1, stop = -1;
            bool stopAfterSection = false;
            if (_settingsFilePath != null && File.Exists(_settingsFilePath)) {
                var settings = File.ReadAllLines(_settingsFilePath); //"manual" ini-file handling to avoid extra dependencies
                var set = settings.FirstOrDefault(s => s.StartsWith("start="));
                if (set != null)
                    start = int.Parse(set.Split('=')[1]);
                set = settings.FirstOrDefault(s => s.StartsWith("stop="));
                if (set != null)
                    stop = int.Parse(set.Split('=')[1]);
                set = settings.FirstOrDefault(s => s.StartsWith("stopAfterSection="));
                if (set != null)
                    stopAfterSection = bool.Parse(set.Split('=')[1]);
            }
            try {
                List<TestDef> tests = GetAllTests(testAssembly);
                if (start != -1 || stop != -1) {
                    var sect = tests.Where(t => t.i >= start && (stop == -1 || t.i <= stop)).ToList();
                    var tid = DateTime.Now;
                    RunTests(sect, filter);
                    var info = new List<string>();
                    info.Add(Environment.MachineName);
                    info.Add(string.Format("Tests run: {0} - {1}", start, stop));
                    info.Add("Starttime: " + tid);
                    info.Add("Elapsed: " + (DateTime.Now - tid));
                    if (ErrorCount > 0) {
                        info.Add("Error count: " + ErrorCount);
                        File.WriteAllLines(_sectionedResultFilePath, info);
                        Thread.Sleep(TimeSpan.FromMinutes(0.2)); //time to allow outside executor handle any files
                    }
                    //writing a log-file with the machinename as filename to be able to compare run times when trying to balance which computers should run which tests
                    File.WriteAllLines(new FileInfo(_settingsFilePath).DirectoryName + @"\" + Environment.MachineName + ".log", info);
                    if (stopAfterSection)
                        tests = new List<TestDef>();
                    else
                        tests = tests.Except(sect).ToList();
                }
                RunTests(tests, filter);
            } finally {
                var procs = ApplicationLauncher.FindProcess();
                foreach (var p in procs) {
                    Log.Debug("Found running application: " + p.ProcessName);
                    try {
                        p.Kill();
                    } catch (Exception ex) {
                        Log.Debug("Error closing: " + ex.Message, ex);
                    }
                }
            }
        }

        private void RunTests(List<TestDef> tests, string filter) {
            Type lastClass = null;
            MethodInfo starter = null, closer = null, classCleanup = null;
            ConstructorInfo constructor = null;

            foreach (var t in tests) {
                Type testclass = t.TestClass;
                //Log.Debug(testclass.FullName);
                if (lastClass != testclass) {
                    MethodInfo[] methods = testclass.GetMethods();
                    starter = (from m in methods
                               where m.Name == "StartApplicationAndLogin"
                               select m).FirstOrDefault();
                    if (starter == null || testclass.IsAbstract)
                        continue;
                    closer = (from m in methods
                              where m.Name == "CloseApplication"
                              select m).FirstOrDefault();

                    constructor = testclass.GetConstructor(Type.EmptyTypes);
                    if (constructor == null) {
                        LogNoConstructorError(testclass.Name);
                        continue;
                    }
                    classCleanup = methods.SingleOrDefault(m => m.IsDefined(typeof(ClassCleanupAttribute), true));
                    lastClass = testclass;
                }
                var classObj = constructor.Invoke(emptyParams);
                RunTestMethod(t.Test, classObj, starter, closer, filter, t.i);
                if (classCleanup != null)
                    classCleanup.Invoke(classObj, emptyParams);
            }
        }

        private void RunTestMethod(MethodInfo testmethod, object classObj, MethodInfo starter, MethodInfo closer, string filter, int i) {
            if (_filenamesThatStopTheTestRun.Any(File.Exists))
                return;
            var completeTestname = classObj + " " + testmethod.Name;
            Log.Debug(i + " " + completeTestname + " " + i + " (" + ErrorCount + ")");
            if ((filter != "" && !testmethod.Name.ToLower().StartsWith(filter)) || FilterByUserHook(completeTestname))
                return;
            if (ResetTestEnvironment != null)
                ResetTestEnvironment();
            try {
                starter.Invoke(classObj, emptyParams);
            } catch (Exception ex) {
                ErrorCount++;
                Log.Error("Error starting program: " + ex.Message, ex);
                //On error wait a bit extra in case the program is hanging
                Thread.Sleep(5000);
                CloseProgram(closer, classObj, emptyParams);
                return;
            }
            try {
                testmethod.Invoke(classObj, emptyParams);
            } catch (Exception ex) {
                ErrorCount++;
                string filename = "";
                try {
                    filename = ScreenShooter.SaveToFile();
                } catch (Exception innerEx) {
                    ErrorCount++;
                    Log.Error("Exception while trying to save screenshot: " + innerEx.Message, innerEx);
                }
                Log.Error(ex.Message + " screenshot: " + filename, ex);
                if (ex.InnerException != null) {
                    Log.Error(ex.InnerException.Message, ex.InnerException);
                }
                try {
                    Log.Error("Latest unique identifiers: " + UiTestDslCoreCommon.UniqueIdentifier + " / " + UiTestDslCoreCommon.shortUnique);
                } catch (Exception) { }
                if (ErrorHook != null)
                    ErrorHook(completeTestname);
            }
            CloseProgram(closer, classObj, emptyParams);
            Log.Debug("-- Test # " + i + " done, current error count: " + ErrorCount + " \n\n");
            //Need to allow the program time to exit, to avoid the next test finding an open program while starting.
            Thread.Sleep(3000);
        }

        private void CloseProgram(MethodInfo closer, object classObj, object[] emptyParams) {
            try {
                closer.Invoke(classObj, emptyParams);
            } catch (Exception ex) {
                ErrorCount++;
                Log.Error("Error closing program: " + ex.Message, ex);
            }
        }
    }
}
