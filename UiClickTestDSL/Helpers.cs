using System;
using System.Collections;
using System.IO;

namespace UiClickTestDSL {
    public static class Helpers {
        public static string ExctractAdditionalInformationFromException(Exception innerException) {
            string res = "";
            if (innerException == null) return string.Empty;
            if (innerException is ArgumentException) {
                var ex = innerException as ArgumentException;
                res += "ParamName: " + ex.ParamName + "\n";
            }
            if (innerException is FileFormatException) {
                res += "File that caused FileFormatException: " + (innerException as FileFormatException).SourceUri + "\n";
            }

            //this is found in all exceptions and covers the possible additional information found in: 
            //    KeyNotFoundException
            //    IndexOutOfRangeException
            foreach (DictionaryEntry entry in innerException.Data) {
                res += " Key: " + entry.Key + " Value: " + entry.Value + "\n";
            }

            if (res.Length > 0) {
                res = "Additional information: " + res + "\n";
            }
            return res;
        }
    }
}
