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

        private List<TestDef> _remainingTests;
        private List<string> _execInfo;

        private string DebugLogPath { get { return new FileInfo(_settingsFilePath).DirectoryName + @"\___" + Environment.MachineName.ToUpper() + ".log"; } }

        public MethodRunner(params string[] filenamesThatStopTheTestRun) {
            _filenamesThatStopTheTestRun = new List<string>(filenamesThatStopTheTestRun);
        }

        public Func<string, bool> TestNameFilterHook = null;
        public Action<string> ErrorHook = null;
        public Action ResetTestEnvironment = null;
        private readonly object[] _emptyParams = { };
        private int _lastTestRun;

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

        public Func<TestDef, int> GetExternalId;
        public Func<string, Func<string, bool>, List<TestDef>, List<TestDef>> FilterTests;
        public Action<TestDef> LogTestRun;
        public Func<TestDef> GetNextSynchronizedTest;
        public Func<TestDef> GetNextTest;

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
                        t.Id = GetExternalId?.Invoke(t) ?? i++;
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
            _execInfo = new List<string>();
            _execInfo.Add(Environment.MachineName.ToUpper());
            _execInfo.Add("Start time: " + startTime);
            try {
                _execInfo.Add("Total marked to be skipped: " + _skipOnThisComputer.Count);
                _remainingTests = GetAllTests(testAssembly, _skipOnThisComputer);
                _execInfo.Add("Total number of tests in Assembly: "+_remainingTests.Count);
                if (GetNextSynchronizedTest != null)
                    RunSynchronizedTests(filter, startTime);
                else
                    RegularTestRun(filter, startTime);
                var endTime = DateTime.Now;
                _execInfo.Add("Total elapsed: " + (endTime - startTime));
            } finally {
                File.WriteAllLines(DebugLogPath, _execInfo);
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

        private void RunSynchronizedTests(string filter, DateTime startTime) {
            _remainingTests = FilterTests(filter, FilterByUserHook, _remainingTests);
            _totalNoTestsToRun = _remainingTests.Count;
            if (_stopAfterSection)
                _totalNoTestsToRun = -1; // We don't know how many tests we will run.
            _execInfo.Add("Starting run of synchronized tests: " + _totalNoTestsToRun + " / " + _remainingTests.Count);
            TestDef t = GetNextSynchronizedTest();
            while (t != null) {
                if (_filenamesThatStopTheTestRun.Any(File.Exists)) {
                    Log.Debug("Found file defined to stop test-run");
                    return;
                }
                InitRunTestAndCleanup(t);
                t = GetNextSynchronizedTest();
            }
            _execInfo.Add("Synchronized tests finished. Run time: " + (DateTime.Now - startTime));
            WriteSectionedResultFiles();
            if (_stopAfterSection)
                return;
            t = GetNextTest();
            while (t != null) {
                if (_filenamesThatStopTheTestRun.Any(File.Exists)) {
                    Log.Debug("Found file defined to stop test-run");
                    return;
                }
                InitRunTestAndCleanup(t);
                t = GetNextTest();
            }
        }

        private void RegularTestRun(string filter, DateTime startTime) {
            _totalNoTestsToRun = _initialTests.Count;
            if (_stopAfterSection)
                _totalNoTestsToRun += _remainingTests.Count(t => t.Id >= _start && (_stop == -1 || t.Id <= _stop));
            else
                _totalNoTestsToRun = _remainingTests.Count;
            if (_initialTests.Any()) {
                var initial = _remainingTests.Where(t => _initialTests.Contains(t.CompleteTestName)).ToList();
                _execInfo.Add($"Starting run of initial tests. # {initial.Count} ({_initialTests.Count})");
                _lastTestRun = RunTests(initial, filter);
                _execInfo.Add("Elapsed: " + (DateTime.Now - startTime) + " last test run: " + _lastTestRun);
                WriteSectionedResultFiles();
                _remainingTests = _remainingTests.Except(initial).ToList();
            }
            if (_start != -1 || _stop != -1) {
                var sect = _remainingTests.Where(t => t.Id >= _start && (_stop == -1 || t.Id <= _stop)).ToList();
                _lastTestRun = RunTests(sect, filter);
                _execInfo.Add($"Sectioned test run: {_start} - {_lastTestRun}; total # run: {sect.Count}");
                _execInfo.Add("Elapsed: " + (DateTime.Now - startTime));
                WriteSectionedResultFiles();
                if (_stopAfterSection)
                    _remainingTests = new List<TestDef>();
                else
                    _remainingTests = _remainingTests.Except(sect).ToList();
            }
            _lastTestRun = RunTests(_remainingTests, filter);
            if (!_stopAfterSection)
                _execInfo.Add($"First and last test run after section: {_remainingTests.OrderBy(t => t.Id).First().Id} - {_lastTestRun}; total # run: {_remainingTests.Count}");
            _execInfo.Add("The sectioned tests was not re-run.");
        }

        private void WriteSectionedResultFiles() {
            _execInfo.Add("Error count: " + ErrorCount);
            if (ErrorCount > 0) {
                File.WriteAllLines(_sectionedResultFilePath, _execInfo);
                Thread.Sleep(TimeSpan.FromMinutes(5)); //time to allow outside executor handle any files
            }
            //writing a log-file with the machinename as filename to be able to compare run times when trying to balance which computers should run which tests
            File.WriteAllLines(DebugLogPath, _execInfo);
        }

        private Type _lastClass = null;
        private MethodInfo _starter = null, _setup = null, _closer = null, _classCleanup = null;
        private ConstructorInfo _constructor = null;
        private int _totalNoTestsToRun = -1;

        private int RunTests(List<TestDef> tests, string filter) {
            foreach (var t in tests) {
                if (_filenamesThatStopTheTestRun.Any(File.Exists)) {
                    Log.Debug("Found file defined to stop test-run");
                    return _lastTestRun;
                }
                if ((!string.IsNullOrEmpty(filter) && !t.Test.Name.ToLower().StartsWith(filter.ToLower())) || FilterByUserHook(t.CompleteTestName))
                    continue;
                InitRunTestAndCleanup(t);
            }
            return _lastTestRun;
        }

        private void InitRunTestAndCleanup(TestDef t) {
            t.HasBeenRun = true; //Needs to be set as early as possible to avoid a test that fails at setup to be run continually
            Type testclass = t.TestClass;
            //Log.Debug(testclass.FullName);
            if (_lastClass != testclass) {
                MethodInfo[] methods = testclass.GetMethods();
                _starter = (from m in methods
                            where m.Name == "StartApplicationAndLogin"
                            select m).FirstOrDefault();
                if (_starter == null || testclass.IsAbstract)
                    return;
                _setup = (from m in methods
                          where m.Name == "SetupEnvironment"
                          select m).FirstOrDefault();
                _closer = (from m in methods
                           where m.Name == "CloseApplication"
                           select m).FirstOrDefault();

                _constructor = testclass.GetConstructor(Type.EmptyTypes);
                if (_constructor == null) {
                    LogNoConstructorError(testclass.Name);
                    return;
                }
                _classCleanup = methods.SingleOrDefault(m => m.IsDefined(typeof(ClassCleanupAttribute), true));
                _lastClass = testclass;
            }
            var classObj = _constructor.Invoke(_emptyParams);
            RunTestMethod(t, classObj);
            _lastTestRun = t.Id;
            if (_classCleanup != null)
                _classCleanup.Invoke(classObj, _emptyParams);
            LogTestRun?.Invoke(t);
        }

        private void LogTestRunError(TestDef test, string msg, Exception ex, object classObj = null, bool screenshot = false, bool close = false) {
            ErrorCount++;
            var logMsg = msg;
            if (screenshot) {
                string filename = "";
                try {
                    filename = ScreenShooter.SaveToFile();
                } catch (Exception innerEx) {
                    ErrorCount++;
                    Log.Error("Exception while trying to save screenshot: " + innerEx.Message, innerEx);
                }
                logMsg += " screenshot: " + filename;
            }

            logMsg += " " + ex.Message;
            Log.Error(logMsg, ex);
            test.ExceptionMsg = logMsg;
            test.Succeded = false;
            if (ex.InnerException != null) {
                Log.Error(ex.InnerException.Message, ex.InnerException);
            }
            try {
                Log.Error("Latest unique identifiers: " + UiTestDslCoreCommon.UniqueIdentifier + " / " + UiTestDslCoreCommon.shortUnique);
            } catch (Exception) { }

            if (close) {
                //On error wait a bit extra in case the program is hanging
                UiTestDslCoreCommon.Sleep(5);
                CloseProgram(classObj, _emptyParams);
            }
        }

        private void RunTestMethod(TestDef test, object classObj) {
            if (_filenamesThatStopTheTestRun.Any(File.Exists)) {
                Log.Debug("Found file defined to stop test-run");
                return;
            }
            test.StartTime = DateTime.Now;
            var testTimer = Stopwatch.StartNew();
            MethodInfo testmethod = test.Test;
            TestsRun++;
            Log.DebugFormat(Environment.NewLine + $"E:{ErrorCount} - {TestsRun}/{_totalNoTestsToRun} - {test.Id} - {test.CompleteTestName}");
            ResetTestEnvironment?.Invoke();
            try {
                _setup.Invoke(classObj, _emptyParams);
            } catch (Exception ex) {
                LogTestRunError(test, "Error setting up environment:", ex);
                return;
            }
            try {
                _starter.Invoke(classObj, _emptyParams);
            } catch (Exception ex) {
                LogTestRunError(test, "Error starting program:", ex, classObj, close: true);
                return;
            }
            test.Startup = testTimer.Elapsed;
            Log.Debug("Startup time: " + test.Startup);
            var runTimer = Stopwatch.StartNew();
            try {
                testmethod.Invoke(classObj, _emptyParams);
                runTimer.Stop();
                Log.Debug("Test run time: " + runTimer.Elapsed);
                test.TestTime = runTimer.Elapsed;
                test.Succeded = true;
            } catch (Exception ex) {
                LogTestRunError(test, "Test run error:", ex, screenshot: true);
                ErrorHook?.Invoke(test.CompleteTestName);
            }
            CloseProgram(classObj, _emptyParams);
            testTimer.Stop();
            Log.Debug($"-- Test # {test.Id} done: {testTimer.Elapsed} \nE: {ErrorCount} \n\n");
            test.TotalTime = testTimer.Elapsed;
            //Need to allow the program time to exit, to avoid the next test finding an open program while starting.
            UiTestDslCoreCommon.Sleep(3);
            test.EndTime = DateTime.Now;
        }

        private void CloseProgram(object classObj, object[] emptyParams) {
            try {
                _closer.Invoke(classObj, emptyParams);
            } catch (Exception ex) {
                ErrorCount++;
                Log.Error("Error closing program: " + ex.Message, ex);
            }
        }
    }
}
