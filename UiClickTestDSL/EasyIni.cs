using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Common {
    //"manual" ini-file handling to avoid extra dependencies
    public class EasyIni {
        private readonly bool _acceptNonExistingFile;
        private string[] _settings = null;

        public EasyIni(string filename, bool acceptNonExistingFile = false) {
            _acceptNonExistingFile = acceptNonExistingFile;
            if (File.Exists(filename))
                _settings = File.ReadAllLines(filename);
        }

        public string Val(string key, string defaultVal = null) {
            if (_acceptNonExistingFile && _settings == null)
                return defaultVal;

            var set = _settings.FirstOrDefault(s => s.StartsWith(key + "=", true, CultureInfo.CurrentCulture));
            if (string.IsNullOrWhiteSpace(set))
                return defaultVal;
            string[] subs = set.Split('=');
            return set.Substring(subs[0].Length + 1);
        }

        public bool Val(string key, bool defaultVal) {
            if (_acceptNonExistingFile && _settings == null)
                return defaultVal;

            var set = _settings.FirstOrDefault(s => s.StartsWith(key + "=", true, CultureInfo.CurrentCulture));
            if (string.IsNullOrWhiteSpace(set))
                return defaultVal;
            return bool.Parse(set.Split('=')[1]);
        }

        public int Val(string key, int defaultVal) {
            if (_acceptNonExistingFile && _settings == null)
                return defaultVal;

            var set = _settings.FirstOrDefault(s => s.StartsWith(key + "=", true, CultureInfo.CurrentCulture));
            if (string.IsNullOrWhiteSpace(set))
                return defaultVal;
            return int.Parse(set.Split('=')[1]);
        }

        public List<string> ReadFilteredLinesFromFile(string key) {
            string file = Val(key);
            if (file != null)
                return File.ReadAllLines(file).Where(t => !(string.IsNullOrWhiteSpace(t) || t.StartsWith("--"))).ToList();
            return new List<string>();
        }
    }
}
