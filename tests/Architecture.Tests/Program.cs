using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Architecture.Tests;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("============================================================");
        Console.WriteLine(" ZAINX WORKFORCE — PHASE 2 INTEGRATION & SECURITY SUITE");
        Console.WriteLine("============================================================");

        var stopwatch = Stopwatch.StartNew();
        int passed = 0;
        int failed = 0;

        var testSuites = new object[]
        {
            new BoundaryTests(),
            new Phase2DomainTests(),
            new Phase2SecurityIntegrationTests()
        };

        foreach (var suite in testSuites)
        {
            Console.WriteLine($"\n[SUITE] {suite.GetType().Name}");
            var methods = suite.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                if (method.DeclaringType != suite.GetType()) continue;

                try
                {
                    if (method.ReturnType == typeof(Task))
                    {
                        var task = (Task)method.Invoke(suite, null)!;
                        await task;
                    }
                    else
                    {
                        method.Invoke(suite, null);
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("  [PASS] ");
                    Console.ResetColor();
                    Console.WriteLine(method.Name);
                    passed++;
                }
                catch (Exception ex)
                {
                    var inner = ex.InnerException ?? ex;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("  [FAIL] ");
                    Console.ResetColor();
                    Console.WriteLine($"{method.Name}: {inner.Message}");
                    Console.WriteLine(inner.StackTrace);
                    failed++;
                }
            }
        }

        stopwatch.Stop();
        Console.WriteLine("\n------------------------------------------------------------");
        Console.WriteLine($"Results: Total: {passed + failed}, Passed: {passed}, Failed: {failed} (Duration: {stopwatch.ElapsedMilliseconds}ms)");
        Console.WriteLine("============================================================");

        return failed == 0 ? 0 : 1;
    }
}
