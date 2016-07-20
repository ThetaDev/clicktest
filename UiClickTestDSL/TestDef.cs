using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace UiClickTestDSL {
    public class TestDef {
        public int i;
        public Type TestClass;
        public MethodInfo Test;

        public string CompleteTestName {
            get { return TestClass.FullName + " " + Test.Name; }
        }
    }
}
