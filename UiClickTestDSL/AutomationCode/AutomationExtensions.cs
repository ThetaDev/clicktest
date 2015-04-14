using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Automation;

namespace UiClickTestDSL.AutomationCode {
    public static class AutomationExtensions {
        private static readonly Dictionary<string, List<string>> NameOptions = new Dictionary<string, List<string>> {
                { "Cancel", new List<string>{ "Avbryt" } },
                { "No", new List<string>{ "Nei" } },     
                { "Open", new List<string>{ "Åpne" } },
                { "Save", new List<string>{ "Lagre" } },
                { "_Update", new List<string>{ "Update" } },
                { "Yes", new List<string>{ "Ja" } },
                { "Items View", new List<string>{ "Elementvisning" }}
        };

        public static void AddNameOption(string what, params string[] options) {
            if (!NameOptions.ContainsKey(what))
                NameOptions[what] = new List<string>();
            foreach (var option in options) {
                NameOptions[what].Add(option);
            }
        }

        private static PropertyCondition AutomationId(string id) {
            return new PropertyCondition(AutomationElement.AutomationIdProperty, id);
        }

        private static PropertyCondition ControlType(ControlType controlType) {
            return new PropertyCondition(AutomationElement.ControlTypeProperty, controlType);
        }

        private static PropertyCondition LocalizedControlType(string controlType) {
            return new PropertyCondition(AutomationElement.LocalizedControlTypeProperty, controlType);
        }

        private static OrCondition Or(IEnumerable<Condition> conditions) {
            return new OrCondition(conditions.ToArray());
        }

        private static PropertyCondition ClassName(string name) {
            return new PropertyCondition(AutomationElement.ClassNameProperty, name);
        }

        private static PropertyCondition Name(string name) {
            return new PropertyCondition(AutomationElement.NameProperty, name);
        }

        public static AutomationElement FindChildByControlTypeAndAutomationId(this AutomationElement element, ControlType controlType, string automationId) {
            return RunActualSearch(element, AutomationId(automationId), ControlType(controlType));
        }

        public static AutomationElement FindChildByControlTypeAndAutomationIdOrName(this AutomationElement element, ControlType controlType, string automationOrName) {
            List<Condition> nameConds = BuildNameOptionList(automationOrName);
            nameConds.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, automationOrName));
            return RunActualSearch(element, Or(nameConds), ControlType(controlType));
        }

        public static AutomationElement FindChildByControlTypeAndAutomationIdAndName(this AutomationElement element, ControlType controlType, string automationId, string name) {
            return RunSearchWithName(element, name, AutomationId(automationId), ControlType(controlType));
        }

        public static AutomationElement FindChildByControlTypeAndName(this AutomationElement element, ControlType controlType, string name) {
            return RunSearchWithName(element, name, ControlType(controlType));
        }

        public static AutomationElement FindChildByClass(this AutomationElement element, string className) {
            return RunSearchWithName(element, "", ClassName(className));
        }

        public static AutomationElement FindChildByClassAndName(this AutomationElement element, string className, string name) {
            return RunSearchWithName(element, name, ClassName(className));
        }

        public static AutomationElement RunSearchWithName(AutomationElement element, string name, params Condition[] otherSearchConditions) {
            List<Condition> nameConds = BuildNameOptionList(name);
            var temp = new List<Condition>(otherSearchConditions);
            temp.Add(nameConds.Count > 1 ? Or(nameConds) : nameConds[0]);
            return RunActualSearch(element, temp.ToArray());
        }

        private static List<Condition> BuildNameOptionList(string name) {
            var nameConds = new List<Condition> {
                Name(name),
            };
            if (NameOptions.ContainsKey(name)) {
                foreach (var nameOption in NameOptions[name]) {
                    nameConds.Add(Name(nameOption));
                }
            }
            return nameConds;
        }

        internal static AutomationElement RunActualSearch(AutomationElement element, params Condition[] searchConditions) {
            var searchCond = new AndCondition(searchConditions);
            AutomationElement result = null;
            int retries = 20; //= 10sekund ventetid, som typisk kan komme når applikasjonen åpnes
            while (retries > 0) {
                result = element.FindFirst(TreeScope.Descendants, searchCond);
                if (result != null)
                    break;
                retries--;
                Thread.Sleep(500);
            }
            if (result == null)
                throw new AutomationElementNotFoundException("Could not find element: ", searchConditions);
            return result;
        }

        public static AutomationElement FindChildByLocalizedControlTypeAndName(this AutomationElement element, string caption, params string[] controlTypes) {
            return RunSearchWithName(element, caption, Or(controlTypes.Select(LocalizedControlType)));
        }

        public static AutomationElement FindChildByClassAndAutomationId(this AutomationElement element, string classname, string automationId) {
            return RunActualSearch(element, AutomationId(automationId), ClassName(classname));
        }

        public static AutomationElementCollection FindAllChildrenByAutomationId(this AutomationElement element, string automationId) {
            var res = element.FindAll(TreeScope.Descendants, AutomationId(automationId));
            return res;
        }

        public static IEnumerable<AutomationElement> FindAllChildrenByClassName(this AutomationElement element, string classname) {
            var res = element.FindAll(TreeScope.Descendants, ClassName(classname));
            return res.Cast<AutomationElement>();
        }
        public static IEnumerable<AutomationElement> FindAllChildrenByName(this AutomationElement element, string name) {
            var res = element.FindAll(TreeScope.Descendants, Name(name));
            return res.Cast<AutomationElement>();
        }

        public static IEnumerable<AutomationElement> FindAllChildrenByControlType(this AutomationElement element, ControlType controlType) {
            var res = element.FindAll(TreeScope.Descendants, ControlType(controlType));
            return res.Cast<AutomationElement>();
        }

        public static IEnumerable<AutomationElement> FindChildrenByControlType(this AutomationElement element, ControlType controlType) {
            var res = element.FindAll(TreeScope.Children, ControlType(controlType));
            return res.Cast<AutomationElement>();
        }
        public static IEnumerable<AutomationElement> FindAllChildrenByByLocalizedControlType(this AutomationElement element, string controlType) {
            var res = element.FindAll(TreeScope.Descendants, LocalizedControlType(controlType));
            return res.Cast<AutomationElement>();
        }

        //http://blog.functionalfun.net/2009/06/introduction-to-ui-automation-with.html
        public static AutomationElement FindChildByProcessId(this AutomationElement element, int processId) {
            var result = element.FindChildByCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, processId));
            return result;
        }

        public static AutomationElement FindChildByCondition(this AutomationElement element, Condition condition) {
            var result = element.FindFirst(TreeScope.Children, condition);
            return result;
        }

        public static AutomationElement FindDescendentByIdPath(this AutomationElement element, IEnumerable<string> idPath) {
            var conditionPath = CreateConditionPathForPropertyValues(AutomationElement.AutomationIdProperty, idPath);
            return FindDescendentByConditionPath(element, conditionPath);
        }

        public static IEnumerable<Condition> CreateConditionPathForPropertyValues(AutomationProperty property, IEnumerable<object> values) {
            var conditions = values.Select(value => new PropertyCondition(property, value));
            return conditions;
        }

        public static AutomationElement FindDescendentByConditionPath(this AutomationElement element, IEnumerable<Condition> conditionPath) {
            if (!conditionPath.Any()) {
                return element;
            }
            var result = conditionPath.Aggregate(element, (parentElement, nextCondition) => parentElement == null ? null : parentElement.FindChildByCondition(nextCondition));
            return result;
        }
    }
}
