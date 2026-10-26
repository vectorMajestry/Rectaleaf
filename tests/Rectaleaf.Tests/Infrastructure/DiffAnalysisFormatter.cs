namespace Rectaleaf.Tests.Infrastructure;

public sealed class DiffAnalysisFormatter
{
    private readonly ConsoleWriter console;
    private readonly OutputVerbosity verbosity;

    public DiffAnalysisFormatter(ConsoleWriter console, OutputVerbosity verbosity = OutputVerbosity.Normal)
    {
        this.console = console;
        this.verbosity = verbosity;
    }

    public void FormatCaseStart(string caseName, string baselineLabel, string targetLabel)
    {
        if (verbosity >= OutputVerbosity.Normal)
            console.WriteLine($"\nRunning Case: {caseName} (Comparing {targetLabel} to {baselineLabel})...");
    }

    public void FormatCaseResult(string caseName, DiffReport report, string baselineLabel, string targetLabel)
    {
        if (!report.HasDifferences)
        {
            if (verbosity >= OutputVerbosity.Normal)
            {
                console.Write("  [PASS] ", ConsoleColor.Green);
                console.WriteLine(caseName);
            }
            return;
        }

        console.Write("  [FAIL] ", ConsoleColor.Red);
        console.WriteLine($"{caseName} ({report.Differences.Count} difference{(report.Differences.Count == 1 ? "" : "s")})");

        if (verbosity == OutputVerbosity.Quiet)
            return;

        FormatDifferences(report.Differences, baselineLabel, targetLabel);
    }

    public void FormatCaseError(string caseName, string error)
    {
        console.Write("  [FAIL] ", ConsoleColor.Red);
        console.WriteLine(caseName);
        if (verbosity >= OutputVerbosity.Normal)
            console.WriteLine($"      Error: {error}", ConsoleColor.Red);
    }

    public void FormatDifferences(IReadOnlyList<Difference> differences, string baselineLabel, string targetLabel)
    {
        string headerBaseline = (Capitalize(baselineLabel) + ":").PadRight(14);
        string headerTarget = (Capitalize(targetLabel) + ":").PadRight(14);

        for (int i = 0; i < differences.Count; i++)
        {
            Difference diff = differences[i];
            console.WriteLine();
            console.Write($"  #{i + 1} ", ConsoleColor.DarkGray);

            if (verbosity == OutputVerbosity.Detailed)
            {
                console.WriteLine(diff.HeaderDetailed, ConsoleColor.Yellow);
                console.WriteLine($"      Path:         {diff.FormattedPathDetailed}", ConsoleColor.DarkYellow);
            }
            else
            {
                console.WriteLine(diff.HeaderNormal, ConsoleColor.Yellow);
            }

            console.WriteLine($"      {headerBaseline}{diff.Expected}", ConsoleColor.Green);
            console.WriteLine($"      {headerTarget}{diff.Actual}", ConsoleColor.Red);
        }
    }

    public void FormatSummary(int totalCases, int passedCases, int failedCases, int totalDifferences)
    {
        if (verbosity == OutputVerbosity.Quiet)
        {
            if (failedCases > 0)
                console.WriteErrorLine($"FAILED: {failedCases}/{totalCases} case(s) failed with {totalDifferences} difference(s).");
            return;
        }

        console.WriteLine();
        console.WriteLine("-----");
        if (failedCases == 0)
        {
            console.WriteLine($"SUCCESS: All {totalCases} test case(s) passed with 0 differences.", ConsoleColor.Green);
        }
        else
        {
            console.WriteLine($"FAILURE: {failedCases} of {totalCases} case(s) failed with {totalDifferences} total difference(s).", ConsoleColor.Red);
        }
        console.WriteLine("-----");
        console.WriteLine();
    }

    private static string Capitalize(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToUpperInvariant(str[0]) + str[1..];
    }
}
