using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Project.Tests.Common
{
    /// <summary>
    /// Shared test output logic used by both UnitTests and Integration base classes.
    /// Prints: >> TestName, Expected/Actual on failure, PASSED/FAILED.
    /// </summary>
    public static class TestOutputHelper
    {
        public static void LogStart()
        {
            try { TestContext.Progress.WriteLine($"\n  >> {TestContext.CurrentContext.Test.Name}"); }
            catch { }
        }

        public static void LogEnd()
        {
            try
            {
                var result = TestContext.CurrentContext.Result;
                if (result.Outcome.Status == TestStatus.Passed)
                    { TestContext.Progress.WriteLine("     PASSED"); return; }
                if (!string.IsNullOrWhiteSpace(result.Message))
                    PrintFailure(result.Message);
                TestContext.Progress.WriteLine("     FAILED");
            }
            catch { }
        }

        private static void PrintFailure(string message)
        {
            string? expected = null, actual = null;
            foreach (var raw in message.Split('\n'))
            {
                var line = raw.Trim();
                if      (line.StartsWith("Expected:")) expected = line.Substring(9).Trim();
                else if (line.StartsWith("But was:"))  actual   = line.Substring(8).Trim();
                else if (line.StartsWith("Actually:")) actual   = line.Substring(9).Trim();
            }
            if (expected != null && actual != null)
            {
                TestContext.Progress.WriteLine($"     Expected : {expected}");
                TestContext.Progress.WriteLine($"     Actual   : {actual}");
            }
            else
            {
                foreach (var raw in message.Split('\n'))
                {
                    var l = raw.Trim();
                    if (!string.IsNullOrEmpty(l)) { TestContext.Progress.WriteLine($"     {l}"); break; }
                }
            }
        }
    }
}
