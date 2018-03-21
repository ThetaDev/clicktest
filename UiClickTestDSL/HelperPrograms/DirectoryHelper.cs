using System;
using System.IO;

namespace UiClickTestDSL.HelperPrograms {
    public static class DirectoryHelper {
        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// </summary>
        public static void DeleteDirectory(string path) {
            //ref: https://stackoverflow.com/a/1703799/4353819
            foreach (string directory in Directory.GetDirectories(path)) {
                DeleteDirectory(directory);
            }

            try {
                Directory.Delete(path, true);
            } catch (IOException) {
                Directory.Delete(path, true);
            } catch (UnauthorizedAccessException) {
                Directory.Delete(path, true);
            }
        }
    }
}
