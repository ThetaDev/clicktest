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
            if (searchCondition is PropertyCondition) {
                res.Append((searchCondition as PropertyCondition).Property + " ");
                res.Append((searchCondition as PropertyCondition).Value + " ");
            } else if (searchCondition is OrCondition) {
                foreach (var cond in (searchCondition as OrCondition).GetConditions())
                    HandleCondition(cond, res);
            } else if (searchCondition is AndCondition) {
                foreach (var cond in (searchCondition as AndCondition).GetConditions())
                    HandleCondition(cond, res);
            } else {
                res.Append(searchCondition);
            }
        }
    }
}
