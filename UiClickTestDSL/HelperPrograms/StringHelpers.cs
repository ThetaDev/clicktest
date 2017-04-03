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
    }
}
