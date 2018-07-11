using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiClickTestDSL.HelperPrograms {
    public static class StringHelpers {
        public static bool ContainsIgnoreCase(this string text, string substring) {
            //string.Contains has no overload for ignoring case, so using IndexOf instead.
            return text.IndexOf(substring, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
        public static int SubstringCount(this string haystack, string needle) {
            //Counts occurrances of "needle" in "haystack".
            return haystack.Split(new[] { needle }, StringSplitOptions.None).Length - 1;
        }
    }
}
