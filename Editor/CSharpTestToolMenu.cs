using UnityEditor;
using UnityEngine;
using System.IO;

namespace CSharpTestToolForUnity.Editor
{
    public static class CSharpTestToolMenu
    {
        private const string PkgName = "com.ogames.csharptesttoolforunity";

        [MenuItem("CPM RP Tools/CSharp Test Tool/Setup PowerShell (run once)")]
        public static void SetupPowerShell()
        {
            string path = GetPath();
            if (path == null) { EditorUtility.DisplayDialog("CSharp Test Tool", "Package not found.", "OK"); return; }
            string init = Path.Combine(path, "init.ps1");
            if (!File.Exists(init)) { EditorUtility.DisplayDialog("CSharp Test Tool", "init.ps1 not found at:\n" + init, "OK"); return; }
            var p = new System.Diagnostics.ProcessStartInfo();
            p.FileName = "powershell.exe";
            p.Arguments = "-ExecutionPolicy Bypass -NoExit -File \"" + init + "\"";
            p.UseShellExecute = true;
            p.WorkingDirectory = path;
            System.Diagnostics.Process.Start(p);
        }

        [MenuItem("CPM RP Tools/CSharp Test Tool/Open Terminal Here")]
        public static void OpenTerminal()
        {
            string path = GetPath();
            if (path == null) return;
            var p = new System.Diagnostics.ProcessStartInfo();
            p.FileName = "powershell.exe";
            p.Arguments = "-NoExit -Command \"Set-Location '" + path + "'\"";
            p.UseShellExecute = true;
            System.Diagnostics.Process.Start(p);
        }

        [MenuItem("CPM RP Tools/CSharp Test Tool/Run Examples")]
        public static void RunExamples()
        {
            string path = GetPath();
            if (path == null) return;
            var p = new System.Diagnostics.ProcessStartInfo();
            p.FileName = "powershell.exe";
            // Use -Command so 'examples' is passed correctly as an argument
            p.Arguments = "-ExecutionPolicy Bypass -NoExit -Command \"& '" + Path.Combine(path, "test.ps1").Replace("'", "''") + "' examples\"";
            p.UseShellExecute = true;
            p.WorkingDirectory = path;
            System.Diagnostics.Process.Start(p);
        }

        private static string GetPath()
        {
            string local = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", PkgName));
            if (Directory.Exists(local) && File.Exists(Path.Combine(local, "package.json"))) return local;
            string cache = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));
            if (Directory.Exists(cache))
                foreach (var d in Directory.GetDirectories(cache))
                    if (Path.GetFileName(d).StartsWith(PkgName) && File.Exists(Path.Combine(d, "package.json")))
                        return d;
            if (File.Exists(Path.Combine(Application.dataPath, "package.json")))
                return Application.dataPath;
            return null;
        }
    }
}
