using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Automation;

namespace UiClickTestDSL.AutomationCode {
    public class AutomationElementNotFoundException : Exception {
        public IEnumerable<Condition> SearchConditions { get; private set; }

        public AutomationElementNotFoundException(string message, IEnumerable<Condition> searchConditions)
            : base(message + SearchConditionsToString(searchConditions)) {
            SearchConditions = searchConditions;
        }

        private static string SearchConditionsToString(IEnumerable<Condition> searchConditions) {
            var res = new StringBuilder();
            foreach (var searchCondition in searchConditions) {
                HandleCondition(searchCondition, res);
            }
            return res.ToString();
        }

        private static void HandleCondition(Condition searchCondition, StringBuilder res) {
            res.Append("(");
            if (searchCondition is PropertyCondition prop) {
                var readablePropName = prop.Property.ProgrammaticName.Replace("AutomationElementIdentifiers.", "");
                res.Append(readablePropName);
                if (prop.Flags == PropertyConditionFlags.IgnoreCase) {
                    res.Append("[IgnoreCase]");
                }
                res.Append(": ");
                res.Append(GetStringFromPropertyConditionValue(prop));
            } else if (searchCondition is OrCondition or) {
                foreach (var sub in or.GetConditions()) {
                    HandleCondition(sub, res);
                    res.Append(" OR ");
                }
                res.Length -= 4; //" OR "
            } else if (searchCondition is AndCondition and) {
                foreach (var sub in and.GetConditions()) {
                    HandleCondition(sub, res);
                    res.Append(" AND ");
                }
                res.Length -= 5; //" AND "
            } else {
                res.Append(searchCondition);
            }

            res.Append(")");
        }
        private static string GetStringFromPropertyConditionValue(PropertyCondition prop) {
            if (Equals(prop.Property, AutomationElement.ControlTypeProperty) && prop.Value is int id) {
                var controlType = ControlType.LookupById(id);
                if (controlType != null) {
                    return controlType.ProgrammaticName;
                }
            }
            return prop.Value as string;
        }
    }
}
