using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using Microsoft.Test.Input;
using UiClickTestDSL.AutomationCode;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using UiClickTestDSL.DslObjects;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Media;

namespace UiClickTestDSL.AutomationCode {
    public static class AutomationExtensions {
        private static readonly Dictionary<string, List<string>> NameOptions = new Dictionary<string, List<string>> {
                { "Cancel", new List<string>{ "Avbryt" } },
                { "No", new List<string>{ "Nei" } },
                { "Open", new List<string>{ "Åpne" } },
                { "Save", new List<string>{ "Lagre" } },
                { "_Update", new List<string>{ "Update" } },
                { "Yes", new List<string>{ "Ja" } },
                { "Items View", new List<string>{ "Elementvisning" }},
                { "OK", new List<string>{ "Ok" }},
                { "File name:", new List<string>{ "Filnavn:" }}
        };

        public static string[] DialogLocalizedControlNameOptions = { //different name options on different language settings
            "Dialog", "dialog", "Dialogue", "dialogue", "Window", "window", "Dialogboks", "dialogboks", "Vindu", "vindu"
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
            return new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase);
        }

        public static AutomationElement FindChildByControlTypeAndClass(this AutomationElement element, ControlType controlType, string classname) {
            return RunActualSearch(element, false, ClassName(classname), ControlType(controlType));
        }

        public static AutomationElement FindChildByControlTypeAndAutomationId(this AutomationElement element, ControlType controlType, string automationId) {
            return RunActualSearch(element, false, AutomationId(automationId), ControlType(controlType));
        }

        public static AutomationElement FindChildByControlTypeAndAutomationIdOrName(this AutomationElement element, ControlType controlType, string automationOrName) {
            List<Condition> nameConds = BuildNameOptionList(automationOrName);
            nameConds.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, automationOrName));
            return RunActualSearch(element, false, Or(nameConds), ControlType(controlType));
        }

        public static AutomationElement FindChildByControlTypeAndAutomationIdAndName(this AutomationElement element, ControlType controlType, string automationId, string name) {
            return RunSearchWithName(element, name, false, AutomationId(automationId), ControlType(controlType));
        }

        public static AutomationElement FindChildByControlTypeAndName(this AutomationElement element, ControlType controlType, string name) {
            return RunSearchWithName(element, name, false, ControlType(controlType));
        }

        public static AutomationElement FindChildByClass(this AutomationElement element, string className) {
            return RunSearchWithName(element, "", false, ClassName(className));
        }

        public static AutomationElement FindChildByClassAndName(this AutomationElement element, string className, string name) {
            return RunSearchWithName(element, name, false, ClassName(className));
        }

        public static AutomationElement RunSearchWithName(AutomationElement element, string name, bool quickCheck, params Condition[] otherSearchConditions) {
            List<Condition> nameConds = BuildNameOptionList(name);
            var temp = new List<Condition>(otherSearchConditions);
            temp.Add(nameConds.Count > 1 ? Or(nameConds) : nameConds[0]);
            return RunActualSearch(element, quickCheck, temp.ToArray());
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

        internal static AutomationElement RunActualSearch(AutomationElement element, bool quickCheck, params Condition[] searchConditions) {
            var searchCond = new AndCondition(searchConditions);
            AutomationElement result = null;
            int retries = 40;
            if (quickCheck)
                retries = 8; //total of 2seconds sleep + searching = about 10 seconds search-time.
            while (retries > 0) {
                result = element.FindFirst(TreeScope.Descendants, searchCond);
                if (result != null)
                    break;
                retries--;
                UiTestDslCoreCommon.SleepMilliseconds(250);
            }
            if (result == null) {
                throw new AutomationElementNotFoundException("Could not find element: ", new[] { searchCond });
            }
            return result;
        }

        public static AutomationElement FindChildByLocalizedControlTypeAndName(this AutomationElement element, string caption, bool quickCheck, params string[] controlTypes) {
            return RunSearchWithName(element, caption, quickCheck, Or(controlTypes.Select(LocalizedControlType)));
        }

        public static AutomationElement FindChildByClassAndAutomationId(this AutomationElement element, string classname, string automationId) {
            return RunActualSearch(element, false, AutomationId(automationId), ClassName(classname));
        }

        public static AutomationElementCollection FindAllChildrenByAutomationId(this AutomationElement element, string automationId) {
            var res = element.FindAll(TreeScope.Descendants, AutomationId(automationId));
            return res;
        }

        public static AutomationElementCollection FindAllElementChildrenByName(this AutomationElement element, string name) {
            var res = element.FindAll(TreeScope.Descendants, Name(name));
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
        public static IEnumerable<AutomationElement> FindAllChildrenByByLocalizedControlType(this AutomationElement element, params string[] controlTypes) {
            var res = element.FindAll(TreeScope.Descendants, Or(controlTypes.Select(LocalizedControlType)));
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

        public static bool HasChildByClass(this AutomationElement element, string className) {
            return RunHasSearchWithName(element, "", ClassName(className));
        }

        public static bool RunHasSearchWithName(AutomationElement element, string name, params Condition[] otherSearchConditions) {
            List<Condition> nameConds = BuildNameOptionList(name);
            var temp = new List<Condition>(otherSearchConditions);
            temp.Add(nameConds.Count > 1 ? Or(nameConds) : nameConds[0]);
            return RunActualHasSearch(element, temp.ToArray());
        }

        internal static bool RunActualHasSearch(AutomationElement element, params Condition[] searchConditions) {
            var searchCond = new AndCondition(searchConditions);
            int retries = 40;
            while (retries > 0) {
                var result = element.FindFirst(TreeScope.Descendants, searchCond);
                if (result != null)
                    return true;
                retries--;
                UiTestDslCoreCommon.SleepMilliseconds(250);
            }
            return false;
        }
    }
}
