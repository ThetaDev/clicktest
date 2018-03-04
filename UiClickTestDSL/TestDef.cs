using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UiClickTestDSL {
    public class TestDef {
        public int Id;
        public Type TestClass;
        private string _testClassName;
        public MethodInfo Test;
        private string _testName;
        public DateTime StartTime;
        public DateTime EndTime;
        public TimeSpan Startup;
        public TimeSpan TestTime;
        public TimeSpan TotalTime;
        public bool Succeded;
        public bool HasBeenRun;
        public string ExceptionMsg;

        public string ClassName {
            get => _testClassName ?? TestClass.FullName;
            set => _testClassName = value;
        }

        public string Name {
            get => _testName ?? Test.Name;
            set => _testName = value;
        }

        public string CompleteTestName {
            get => TestClass.FullName + " " + Test.Name;
        }
    }
}
