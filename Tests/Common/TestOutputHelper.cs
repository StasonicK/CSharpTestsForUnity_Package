using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Project.Tests.Common
{
    public static class TestOutputHelper
    {
        // Per-process static: resets between dotnet test invocations (new process each time).
        // [ThreadStatic] would be needed for parallel test execution.
        [System.ThreadStatic] private static string? _lastClass;

        public static void LogStart(Type testClass)
        {
            try
            {
                if (_lastClass != testClass.Name)
                {
                    _lastClass = testClass.Name;
                    Console.WriteLine($"\n  ---- {testClass.Name} ----");
                }
                Console.WriteLine($"  >> {TestContext.CurrentContext.Test.Name}");
            }
            catch { }
        }

        public static void LogEnd()
        {
            try
            {
                var r = TestContext.CurrentContext.Result;
                if (r.Outcome.Status == TestStatus.Passed) { Console.WriteLine("     PASSED"); return; }
                if (!string.IsNullOrWhiteSpace(r.Message)) PrintFailure(r.Message);
                Console.WriteLine("     FAILED");
            }
            catch { }
        }

        private static void PrintFailure(string msg)
        {
            string? exp = null, act = null;
            foreach (var raw in msg.Split('\n'))
            {
                var l = raw.Trim();
                if (l.StartsWith("Expected:")) exp = l.Substring(9).Trim();
                else if (l.StartsWith("But was:")) act = l.Substring(8).Trim();
                else if (l.StartsWith("Actually:")) act = l.Substring(9).Trim();
            }
            if (exp != null && act != null) { Console.WriteLine($"     Expected : {exp}"); Console.WriteLine($"     Actual   : {act}"); }
            else foreach (var raw in msg.Split('\n')) { var l=raw.Trim(); if(!string.IsNullOrEmpty(l)){Console.WriteLine($"     {l}");break;} }
        }
    }
}
