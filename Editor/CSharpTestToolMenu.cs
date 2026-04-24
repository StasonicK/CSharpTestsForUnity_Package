using UnityEditor;
using UnityEngine;
using System.IO;

namespace CSharpTestToolForUnity.Editor
{
    /// <summary>
    /// CPM RP Tools > CSharp Test Tool menu.
    /// All actions open CMD (no PowerShell required).
    /// Finds the package path automatically — works from Packages/ or Library/PackageCache/.
    /// </summary>
    public static class CSharpTestToolMenu
    {
        private const string PkgName = "com.ogames.csharptesttoolforunity";

        /// <summary>Opens CMD in the package folder. Type 'test examples', 'test all', etc.</summary>
        [MenuItem("CPM RP Tools/CSharp Test Tool/Open Terminal Here")]
        public static void OpenTerminal()
        {
            string path = GetPath();
            if (path == null) { ShowNotFound(); return; }
            var p = new System.Diagnostics.ProcessStartInfo();
            p.FileName = "cmd.exe";
            p.Arguments = "/k \"cd /d \"" + path + "\"\"";
            p.UseShellExecute = true;
            System.Diagnostics.Process.Start(p);
        }

        /// <summary>Opens CMD in the package folder and runs 'test examples' immediately.</summary>
        [MenuItem("CPM RP Tools/CSharp Test Tool/Run Examples")]
        public static void RunExamples()
        {
            string path = GetPath();
            if (path == null) { ShowNotFound(); return; }
            var p = new System.Diagnostics.ProcessStartInfo();
            p.FileName = "cmd.exe";
            p.Arguments = "/k \"cd /d \"" + path + "\" && test examples\"";
            p.UseShellExecute = true;
            System.Diagnostics.Process.Start(p);
        }

        /// <summary>Opens CMD and runs 'test info' to show available commands.</summary>
        [MenuItem("CPM RP Tools/CSharp Test Tool/Show Help")]
        public static void ShowHelp()
        {
            string path = GetPath();
            if (path == null) { ShowNotFound(); return; }
            var p = new System.Diagnostics.ProcessStartInfo();
            p.FileName = "cmd.exe";
            p.Arguments = "/k \"cd /d \"" + path + "\" && test info\"";
            p.UseShellExecute = true;
            System.Diagnostics.Process.Start(p);
        }

        // ── Path resolution ───────────────────────────────────────────────

        private static string GetPath()
        {
            // 1. Packages/ (local or embedded)
            string local = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", PkgName));
            if (Directory.Exists(local) && File.Exists(Path.Combine(local, "package.json"))) return local;

            // 2. Library/PackageCache/ (git or registry — has @hash suffix)
            string cache = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "PackageCache"));
            if (Directory.Exists(cache))
                foreach (var d in Directory.GetDirectories(cache))
                    if (Path.GetFileName(d).StartsWith(PkgName) && File.Exists(Path.Combine(d, "package.json")))
                        return d;

            // 3. Assets/ (development project)
            if (File.Exists(Path.Combine(Application.dataPath, "package.json")))
                return Application.dataPath;

            return null;
        }

        private static void ShowNotFound()
        {
            EditorUtility.DisplayDialog("CSharp Test Tool",
                "Package not found: " + PkgName, "OK");
        }
    }
}
