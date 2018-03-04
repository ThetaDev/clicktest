using System;
using System.IO;
using System.Reflection;

namespace UiClickTestDSL {
    public class FileLocator {
        public static readonly string AssemblyDir = string.Format(@"{0}\", Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6));

        public static DirectoryInfo LocateFile(string filename) {
            var directory = new DirectoryInfo(AssemblyDir);
            while (!File.Exists(directory.FullName + Path.DirectorySeparatorChar + filename)) {
                directory = Directory.GetParent(directory.FullName);
                if (directory == null) {
                    throw new Exception(filename + " not found, starting from " + AssemblyDir);
                }
            }
            return directory;
        }

        public static string EnsureEndsWithDirectorySeparator(string path) {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path;
            return path + Path.DirectorySeparatorChar;
        }

        public static FileInfo LocateFileInfo(string filename) {
            DirectoryInfo directory = LocateFile(filename);
            return new FileInfo(EnsureEndsWithDirectorySeparator(directory.FullName) + filename);
        }

        public static DirectoryInfo LocateFolder(string folderName) {
            var directory = new DirectoryInfo(AssemblyDir);
            while (!Directory.Exists(directory.FullName + Path.DirectorySeparatorChar + folderName)) {
                directory = Directory.GetParent(directory.FullName);
                if (directory == null) {
                    throw new Exception(folderName + " not found, starting from " + AssemblyDir);
                }
            }
            return new DirectoryInfo(directory.FullName + Path.DirectorySeparatorChar + folderName);
        }
    }
}
