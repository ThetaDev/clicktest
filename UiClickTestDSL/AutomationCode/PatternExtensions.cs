using System.Windows.Automation;

namespace UiClickTestDSL.AutomationCode {
    public static class PatternExtensions {
        //http://blog.functionalfun.net/2009/06/introduction-to-ui-automation-with.html
        public static InvokePattern GetInvokePattern(this AutomationElement element) {
            return element.GetPattern<InvokePattern>(InvokePattern.Pattern);
        }

        public static T GetPattern<T>(this AutomationElement element, AutomationPattern pattern) where T : BasePattern {
            var patternObject = element.GetCurrentPattern(pattern);
            return patternObject as T;
        }
    }
}