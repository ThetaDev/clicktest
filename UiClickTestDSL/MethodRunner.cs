﻿using System;
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

        public MethodRunner(params string[] filenamesThatStopTheTestRun) {
            _filenamesThatStopTheTestRun = new List<string>(filenamesThatStopTheTestRun);
        }

        public Func<string, bool> TestNameFilterHook = null;

        private bool FilterByUserHook(string testname) {
            if (TestNameFilterHook != null)
                return TestNameFilterHook(testname);
            return false;
        }

        public void Run(Assembly testAssembly, string filter) {
            int i = 1;
            try {
                var emptyParams = new object[] { };

                Type[] classes = testAssembly.GetTypes();
                foreach (Type testclass in classes) {
                    Log.Debug(testclass.FullName);

                    var methods = testclass.GetMethods();
                    MethodInfo starter = (from m in methods
                                          where m.Name == "StartApplicationAndLogin"
                                          select m).FirstOrDefault();
                    if (starter == null || testclass.IsAbstract)
                        continue;
                    MethodInfo closer = (from m in methods
                                         where m.Name == "CloseApplicaiton"
                                         select m).FirstOrDefault();

                    ConstructorInfo constructor = testclass.GetConstructor(Type.EmptyTypes);
                    var classObj = constructor.Invoke(emptyParams);
                    foreach (var testmethod in methods) {
                        if (_filenamesThatStopTheTestRun.Any(File.Exists))
                            return;
                        if ((filter != "" && !testmethod.Name.ToLower().StartsWith(filter)) || FilterByUserHook(testmethod.Name.ToLower()))
                            continue;
                        if (testmethod.IsDefined(typeof(TestMethodAttribute), true)) {
                            Log.Debug(i + " " + classObj + " " + testmethod.Name);
                            i++;
                            try {
                                starter.Invoke(classObj, emptyParams);
                            } catch (Exception ex) {
                                ErrorCount++;
                                Log.Error("Error starting program: " + ex.Message, ex);
                                continue;
                            }
                            try {
                                testmethod.Invoke(classObj, emptyParams);
                            } catch (Exception ex) {
                                ErrorCount++;
                                string filename = "";
                                try {
                                    filename = ScreenShooter.SaveToFile();
                                    ErrorCount++;
                                } catch (Exception innerEx) {
                                    Log.Error("Exception while trying to save screenshot: " + innerEx.Message, innerEx);
                                }
                                Log.Error(ex.Message + " screenshot: " + filename, ex);
                                if (ex.InnerException != null) {
                                    Log.Error(ex.InnerException.Message, ex.InnerException);
                                }
                                try {
                                    Log.Error("Latest unique identifiers: " + UiTestDslCoreCommon.UniqueIdentifier + " / " + UiTestDslCoreCommon.shortUnique);
                                } catch (Exception) { }
                            }
                            try {
                                closer.Invoke(classObj, emptyParams);
                            } catch (Exception ex) {
                                ErrorCount++;
                                Log.Error("Error closing program: " + ex.Message, ex);
                            }
                            Log.Debug("-- Test # " + i + " done, current error count: " + ErrorCount + " \n\n");
                            //Need to allow the program time to exit, to avoid the next test finding an open program while starting.
                            Thread.Sleep(3000);
                        }
                    }
                }
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
    }
}