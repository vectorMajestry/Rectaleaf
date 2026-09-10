using System;
using System.Collections.Generic;

namespace Rectaleaf.Tests.Infrastructure;

public static class ConsoleReporter
{
    public static void PrintHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n=== {title} ===");
        Console.ResetColor();
    }

    public static void PrintCaseSuccess(string caseName)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  [PASS] ");
        Console.ResetColor();
        Console.WriteLine(caseName);
    }

    public static void PrintCaseFailure(string caseName, int diffCount)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  [FAIL] ");
        Console.ResetColor();
        Console.WriteLine($"{caseName} ({diffCount} difference{(diffCount == 1 ? "" : "s")})");
    }

    public static void PrintDifferences(IReadOnlyList<Difference> differences, string labelA = "Expected", string labelB = "Got")
    {
        string headerA = (Capitalize(labelA) + ":").PadRight(14);
        string headerB = (Capitalize(labelB) + ":").PadRight(14);

        for (int i = 0; i < differences.Count; i++)
        {
            var diff = differences[i];
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  #{i + 1} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(diff.EntityDescription);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"      Path:         {diff.Path}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"      {headerA}{diff.Expected}");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"      {headerB}{diff.Actual}");
            Console.ResetColor();
        }
    }

    static string Capitalize(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpperInvariant(str[0]) + str.Substring(1);
    }

    public static void PrintSummary(int totalCases, int passedCases, int failedCases, int totalDifferences)
    {
        Console.WriteLine("\n------------------------------------------------------------");
        if (failedCases == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"SUCCESS: All {totalCases} test case(s) passed with 0 differences.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILURE: {failedCases} of {totalCases} case(s) failed with {totalDifferences} total difference(s).");
        }
        Console.ResetColor();
        Console.WriteLine("------------------------------------------------------------\n");
    }
}
