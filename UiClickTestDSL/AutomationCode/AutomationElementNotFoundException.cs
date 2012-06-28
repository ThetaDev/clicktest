using System;
using System.Collections.Generic;
using System.Windows.Automation;

namespace UiClickTestDSL.AutomationCode {
    public class AutomationElementNotFoundException : Exception {
        public IEnumerable<Condition> SearchConditions { get; private set; }

        public AutomationElementNotFoundException(string message, IEnumerable<Condition> searchConditions)
            : base(message + SearchConditionsToString(searchConditions)) {
            SearchConditions = searchConditions;
        }

        private static string SearchConditionsToString(IEnumerable<Condition> searchConditions) {
            var res = " ";
            foreach (var searchCondition in searchConditions) {
                if (searchCondition is PropertyCondition) {
                    res += (searchCondition as PropertyCondition).Property + " ";
                    res += (searchCondition as PropertyCondition).Value + " ";
                } else {
                    res += searchCondition;
                }
            }
            return res;
        }
    }
}
