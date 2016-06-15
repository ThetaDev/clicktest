namespace UiClickTestDSL.AutomationCode {
    public abstract class By {
        public string Value;
        protected By(string val) {
            Value = val;
        }

        public static ByAutomationId AutomationId(string id) { return new ByAutomationId(id); }
        public static ById Id(string id) { return new ById(id); }
        public static ByName Name(string name) { return new ByName(name); }
    }

    public class ByAutomationId : By { public ByAutomationId(string id) : base(id) { } }

    public class ById : By { public ById(string id) : base(id) { } }

    public class ByName : By { public ByName(string name) : base(name) { } }
}
