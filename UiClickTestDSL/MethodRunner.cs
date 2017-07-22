using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common;
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
        public int TestsRun { get; private set; }
        private readonly List<string> _filenamesThatStopTheTestRun;
        public string _settingsFilePath;
        public string _sectionedResultFilePath;
        public string BundleFilename;

        private string DebugLogPath { get { return new FileInfo(_settingsFilePath).DirectoryName + @"\___" + Environment.MachineName.ToUpper() + ".log"; } }

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

        private int _start = -1, _stop = -1;
        private bool _stopAfterSection = false;
        private List<string> _initialTests = new List<string>();
        private List<string> _skipOnThisComputer = new List<string>();

        private void Init() {
            if (_settingsFilePath != null && File.Exists(_settingsFilePath)) {
                var ini = new EasyIni(_settingsFilePath); //"manual" ini-file handling to avoid extra dependencies
                _start = ini.Val("start", _start);
                _stop = ini.Val("stop", _stop);
                _stopAfterSection = ini.Val("stopAfterSection", _stopAfterSection);
                _initialTests = ini.ReadFilteredLinesFromFile("InitialTestsFile");
                _skipOnThisComputer = ini.ReadFilteredLinesFromFile("SkipOnThisComputerFile");
            }
        }

        private List<TestDef> GetAllTests(Assembly testAssembly, List<string> skipOnThisComputer) {
            Type[] classes = testAssembly.GetTypes();
            List<TestDef> tests = new List<TestDef>();
            int i = 1;
            foreach (Type testclass in classes) {
                if (!testclass.IsDefined(typeof(TestClassAttribute), false))
                    continue;
                MethodInfo[] methods = testclass.GetMethods();
                foreach (MethodInfo testmethod in methods) {
                    if (testmethod.IsDefined(typeof(TestMethodAttribute), true)) {
                        var t = new TestDef { TestClass = testclass, Test = testmethod, };
                        if (skipOnThisComputer.Contains(t.CompleteTestName))
                            continue;
                        t.i = i++;
                        tests.Add(t);
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
            Init();
            var startTime = DateTime.Now;
            var info = new List<string>();
            info.Add(Environment.MachineName.ToUpper());
            info.Add("Start time: " + startTime);
            try {
                info.Add(string.Format("Total marked to be skipped: {0}", _skipOnThisComputer.Count));
                List<TestDef> tests = GetAllTests(testAssembly, _skipOnThisComputer);
                int noToBeRun = _initialTests.Count;
                if (_stopAfterSection)
                    noToBeRun += tests.Count(t => t.i >= _start && (_stop == -1 || t.i <= _stop));
                else
                    noToBeRun = tests.Count;
                int lastTestRun = 0;
                if (_initialTests.Any()) {
                    var initial = tests.Where(t => _initialTests.Contains(t.CompleteTestName)).ToList();
                    info.Add(string.Format("Starting run of initial tests. # {0} ({1})", initial.Count, _initialTests.Count));
                    lastTestRun = RunTests(initial, filter, noToBeRun);
                    info.Add("Elapsed: " + (DateTime.Now - startTime) + " last test run: " + lastTestRun);
                    WriteSectionedResultFiles(info);
                    tests = tests.Except(initial).ToList();
                }
                if (_start != -1 || _stop != -1) {
                    var sect = tests.Where(t => t.i >= _start && (_stop == -1 || t.i <= _stop)).ToList();
                    lastTestRun = RunTests(sect, filter, noToBeRun);
                    info.Add(string.Format("Sectioned test run: {0} - {1}; total # run: {2}", _start, lastTestRun, sect.Count));
                    info.Add("Elapsed: " + (DateTime.Now - startTime));
                    WriteSectionedResultFiles(info);
                    if (_stopAfterSection)
                        tests = new List<TestDef>();
                    else
                        tests = tests.Except(sect).ToList();
                }
                lastTestRun = RunTests(tests, filter, noToBeRun);
                if (!_stopAfterSection)
                    info.Add(string.Format("First and last test run after section: {0} - {1}; total # run: {2}", tests.OrderBy(t => t.i).First().i, lastTestRun, tests.Count));
                info.Add("The sectioned tests was not re-run.");
                info.Add("Total elapsed: " + (DateTime.Now - startTime));
            } finally {
                File.WriteAllLines(DebugLogPath, info);
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

        private void WriteSectionedResultFiles(List<string> info) {
            info.Add("Error count: " + ErrorCount);
            if (ErrorCount > 0) {
                File.WriteAllLines(_sectionedResultFilePath, info);
                Thread.Sleep(TimeSpan.FromMinutes(5)); //time to allow outside executor handle any files
            }
            //writing a log-file with the machinename as filename to be able to compare run times when trying to balance which computers should run which tests
            File.WriteAllLines(DebugLogPath, info);
        }

        private int RunTests(List<TestDef> tests, string filter, int totalNoToBeRun) {
            Type lastClass = null;
            MethodInfo starter = null, setup = null, closer = null, classCleanup = null;
            ConstructorInfo constructor = null;
            int lastTestRun = 0;
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
                    setup = (from m in methods
                             where m.Name == "SetupEnvironment"
                             select m).FirstOrDefault();
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
                RunTestMethod(t, classObj, starter, setup, closer, filter, totalNoToBeRun);
                lastTestRun = t.i;
                if (classCleanup != null)
                    classCleanup.Invoke(classObj, emptyParams);
            }
            return lastTestRun;
        }

        private void RunTestMethod(TestDef test, object classObj, MethodInfo starter, MethodInfo setup, MethodInfo closer, string filter, int totalNoTestsToRun) {
            if (_filenamesThatStopTheTestRun.Any(File.Exists)) {
                Log.Debug("Found file defined to stop test-run");
                return;
            }
            var testTimer = Stopwatch.StartNew();
            MethodInfo testmethod = test.Test;
            TestsRun++;
            Log.DebugFormat(Environment.NewLine + $"E:{ErrorCount} - {TestsRun}/{totalNoTestsToRun} - {test.i} - {test.CompleteTestName}");
            if ((filter != "" && !testmethod.Name.ToLower().StartsWith(filter.ToLower())) || FilterByUserHook(test.CompleteTestName))
                return;
            if (ResetTestEnvironment != null)
                ResetTestEnvironment();
            try {
                setup.Invoke(classObj, emptyParams);
            } catch (Exception ex) {
                ErrorCount++;
                Log.Error("Error setting up environment: " + ex.Message, ex);
                return;
            }
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
                    ErrorHook(test.CompleteTestName);
            }
            CloseProgram(closer, classObj, emptyParams);
            testTimer.Stop();
            Log.Debug($"-- Test # {test.i} done: {testTimer.Elapsed} \nE: {ErrorCount} \n\n");
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
