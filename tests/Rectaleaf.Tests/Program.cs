using Rectaleaf.Reflection;
using Rectaleaf.Roslyn;
using Rectaleaf.Tests.Infrastructure;
using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Rectaleaf.Tests;

public static class Program
{
    public static int Main(string[] args)
    {
        ConsoleWriter console = new();
        console.WriteLine("\n=== Rectaleaf Test Runner ===", ConsoleColor.Cyan);
        RootCommand rootCommand = BuildCommandLine(console);
        return rootCommand.Parse(args).Invoke();
    }

    static Dictionary<string, TargetBackend> backendMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "roslyn", TargetBackend.Roslyn },
        { "reflection", TargetBackend.Reflection }
    };

    static string validBackendsString = $"'{string.Join("', '", backendMap.Keys)}'";

    static RootCommand BuildCommandLine(ConsoleWriter console)
    {
        Option<DirectoryInfo> casesDirOption = new("--cases-dir")
        {
            Description = "Directory containing test cases (defaults to 'Cases' directory in the app directory)",
            DefaultValueFactory = _ => new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "Cases"))
        };

        Option<OutputVerbosity> verbosityOption = new("--verbosity", "-v")
        {
            Description = "Output verbosity: quiet, normal, detailed",
            DefaultValueFactory = _ => OutputVerbosity.Normal
        };

        Option<string[]> casesOption = new("--cases", "-c")
        {
            Description = "Test cases to run (comma-separated or multiple arguments, defaults to all)",
            AllowMultipleArgumentsPerToken = true
        };
        casesOption.Aliases.Add("--case");

        // 'verify' command
        Argument<string> verifyBackendArg = new("backend")
        {
            Description = "Backend to verify against expected snapshots (roslyn, reflection)"
        };

        Command verifyCommand = new("verify", "Verify a backend's output against expected snapshots.")
        {
            verifyBackendArg,
            casesOption,
            casesDirOption,
            verbosityOption
        };
        verifyCommand.Aliases.Add("--verify");

        verifyCommand.SetAction(parseResult =>
        {
            string backend = parseResult.GetValue(verifyBackendArg)!;
            DirectoryInfo casesDir = parseResult.GetValue(casesDirOption)!;
            OutputVerbosity verbosity = parseResult.GetValue(verbosityOption);
            string[]? rawCases = parseResult.GetValue(casesOption);

            ref TargetBackend targetBackendRef = ref CollectionsMarshal.GetValueRefOrNullRef(backendMap, backend);
            bool backendInvalid = false;
            if (Unsafe.IsNullRef(ref targetBackendRef))
            {
                console.WriteErrorLine($"error: Invalid backend '{backend}'. Pick from {validBackendsString}.");
                backendInvalid = true;
            }

            IReadOnlyList<TestCase> selectedCases;
            if (rawCases is null or { Length: 0 })
            {
                if (TryGetAllCases(casesDir, console) is not TestCasesSearchSuccess foundCases || backendInvalid)
                    return 1;

                selectedCases = foundCases.TestCases;
            }
            else
            {
                HashSet<string> requestedCases = ParseRequestedCases(rawCases);
                if (TryGetRequestedCases(casesDir, requestedCases, console) is not TestCasesSearchSuccess foundCases || backendInvalid)
                    return 1;

                if (foundCases.TestCases.Count < requestedCases.Count)
                    PrintMissingCasesWarning(console, requestedCases
                        .Except(foundCases.TestCases.Select(c => c.Name), StringComparer.OrdinalIgnoreCase)
                        .ToList());

                selectedCases = foundCases.TestCases;
            }

            var verifiedCases = selectedCases.OfType<VerifiedTestCase>().ToList();
            if (verifiedCases.Count < selectedCases.Count)
                PrintUnverifiedCasesWarning(console, selectedCases
                    .OfType<NotVerifiedTestCase>()
                    .Select(c => c.Name));

            if (verifiedCases.Count == 0)
            {
                console.WriteErrorLine("error: All specified cases are not verified.");
                return 1;
            }

            DiffAnalysisFormatter formatter = new(console, verbosity);
            ProcessLogger logger = new(console, verbosity);

            return CompareBackendToExpected(targetBackendRef, verifiedCases, formatter, logger);
        });

        // 'compare' command
        Argument<string> targetArg = new("target")
        {
            Description = "Source backend to test (roslyn, reflection)"
        };

        Argument<string> baselineArg = new("baseline")
        {
            Description = "Baseline backend to compare against (roslyn, reflection)"
        };

        Command compareCommand = new("compare", "Compare test outputs between backends.")
        {
            targetArg,
            baselineArg,
            casesOption,
            casesDirOption,
            verbosityOption
        };
        compareCommand.Aliases.Add("--compare");

        compareCommand.SetAction(parseResult =>
        {
            string targetBackendName = parseResult.GetValue(targetArg)!;
            string baselineBackendName = parseResult.GetValue(baselineArg)!;
            DirectoryInfo casesDir = parseResult.GetValue(casesDirOption)!;
            OutputVerbosity verbosity = parseResult.GetValue(verbosityOption);
            string[]? rawCases = parseResult.GetValue(casesOption);

            List<string> invalidBackends = [];

            ref TargetBackend baselineBackend =
                ref CollectionsMarshal.GetValueRefOrNullRef(backendMap, baselineBackendName);
            ref TargetBackend targetBackend =
                ref CollectionsMarshal.GetValueRefOrNullRef(backendMap, targetBackendName);

            if (Unsafe.IsNullRef(ref targetBackend))
                invalidBackends.Add(targetBackendName);
            if (Unsafe.IsNullRef(ref baselineBackend))
                invalidBackends.Add(baselineBackendName);

            bool backendInvalid = false;
            if (invalidBackends.Count > 0)
            {
                string errorListString = $"'{string.Join("', '", invalidBackends)}'";
                console.WriteErrorLine(
                    $"error: Invalid backend{(invalidBackends.Count == 1 ? "" : "s:")} {errorListString}. " +
                    $"Pick from {validBackendsString}.");
                backendInvalid = true;
            }

            IReadOnlyList<TestCase> selectedCases;
            if (rawCases is null or { Length: 0 })
            {
                if (TryGetAllCases(casesDir, console) is not TestCasesSearchSuccess foundCases || backendInvalid)
                    return 1;

                selectedCases = foundCases.TestCases;
            }
            else
            {
                HashSet<string> requestedCases = ParseRequestedCases(rawCases);
                if (TryGetRequestedCases(casesDir, requestedCases, console) is not TestCasesSearchSuccess foundCases || backendInvalid)
                    return 1;

                if (foundCases.TestCases.Count < requestedCases.Count)
                    PrintMissingCasesWarning(console, requestedCases
                        .Except(foundCases.TestCases.Select(c => c.Name), StringComparer.OrdinalIgnoreCase)
                        .ToList());

                selectedCases = foundCases.TestCases;
            }

            DiffAnalysisFormatter formatter = new(console, verbosity);
            ProcessLogger logger = new(console, verbosity);

            return CompareBackendToBackend(targetBackend, baselineBackend, selectedCases, formatter, logger);
        });

        // 'update-snapshots' command
        Argument<string> backendArg = new("backend")
        {
            Description = "Backend to generate snapshots from (roslyn, reflection)"
        };

        Argument<string> targetCaseArg = new("target")
        {
            Description = "Test case name(s) (comma-separated) or 'all' to update snapshots"
        };

        Argument<DirectoryInfo> outputDirArg = new("output-dir")
        {
            Description = "Output directory where expected snapshots will be saved"
        };

        Option<bool> yesOption = new("--yes", "-y")
        {
            Description = "Automatically confirm updating all snapshots"
        };

        Command updateCommand = new("update-snapshots", "Update expected output files from a backend")
        {
            backendArg,
            targetCaseArg,
            outputDirArg,
            casesDirOption,
            yesOption,
            verbosityOption
        };
        updateCommand.Aliases.Add("--update-snapshots");

        updateCommand.SetAction(parseResult =>
        {
            string backend = parseResult.GetValue(backendArg)!;
            string targetCase = parseResult.GetValue(targetCaseArg)!;
            DirectoryInfo outputDir = parseResult.GetValue(outputDirArg)!;
            DirectoryInfo casesDir = parseResult.GetValue(casesDirOption)!;
            bool autoConfirm = parseResult.GetValue(yesOption);
            OutputVerbosity verbosity = parseResult.GetValue(verbosityOption);

            ref TargetBackend targetBackendRef = ref CollectionsMarshal.GetValueRefOrNullRef(backendMap, backend);

            bool backendInvalid = false;
            if (Unsafe.IsNullRef(ref targetBackendRef))
            {
                console.WriteErrorLine($"error: Invalid backend '{backend}'. Pick from {validBackendsString}.");
                backendInvalid = true;
            }

            bool isAll = targetCase.Equals("all", StringComparison.OrdinalIgnoreCase);
            ProcessLogger logger = new(console, verbosity);

            if (isAll)
            {
                if (TryGetAllCases(casesDir, console) is not TestCasesSearchSuccess foundCases || backendInvalid)
                    return 1;

                if (!autoConfirm)
                {
                    console.Write($"Overwrite expected snapshots for all {foundCases.TestCases.Count} test cases using '{backend}'? (y/N): ", ConsoleColor.Cyan);
                    string? response = console.ReadLine();
                    if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase))
                    {
                        console.WriteLine("Update aborted.");
                        return 0;
                    }
                }

                return UpdateSnapshots(targetBackendRef, outputDir, foundCases.TestCases, logger);
            }
            else
            {
                HashSet<string> requestedCases = ParseRequestedCases([targetCase]);

                if (TryGetRequestedCases(casesDir, requestedCases, console) is not TestCasesSearchSuccess foundCases || backendInvalid)
                    return 1;

                if (foundCases.TestCases.Count < requestedCases.Count)
                {
                    PrintMissingCasesWarning(console, requestedCases
                        .Except(foundCases.TestCases.Select(c => c.Name), StringComparer.OrdinalIgnoreCase)
                        .ToList());
                }

                return UpdateSnapshots(targetBackendRef, outputDir, foundCases.TestCases, logger);
            }
        });

        return new RootCommand("Rectaleaf Specification Conformance Test Runner")
        {
            verifyCommand,
            compareCommand,
            updateCommand
        };

        static HashSet<string> ParseRequestedCases(IEnumerable<string> rawCases) => rawCases
            .SelectMany(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        static void PrintCasesDirNotFound(ConsoleWriter console, string path)
            => console.WriteErrorLine($"error: Test cases directory not found at '{path}'.");

        static void PrintMissingCasesWarning(ConsoleWriter console, IEnumerable<string> names)
            => console.WriteLine($"warning: Test case(s) not found: {string.Join(", ", names)}", ConsoleColor.Yellow);

        static void PrintUnverifiedCasesWarning(ConsoleWriter console, IEnumerable<string> names)
            => console.WriteLine($"warning: Skipping test case(s) without expected snapshots: {string.Join(", ", names)}", ConsoleColor.Yellow);

        static void PrintNoCasesFound(ConsoleWriter console, string path)
            => console.WriteErrorLine($"error: No test cases found in '{path}'.");

        static void PrintNoMatchingCases(ConsoleWriter console, IEnumerable<string> requestedCasesNames)
            => console.WriteErrorLine($"error: No test cases found matching: {string.Join(", ", requestedCasesNames)}.");

        static TestCasesSearchResult TryGetRequestedCases(DirectoryInfo casesDir, HashSet<string> requestedCases, ConsoleWriter console)
        {
            if (TryGetAllCases(casesDir, console) is not TestCasesSearchSuccess foundCases)
                return new TestCasesSearchFailure();

            var selectedCases = foundCases.TestCases
                .Where(c => requestedCases.Contains(c.Name))
                .ToList();

            if (selectedCases.Count == 0)
            {
                PrintNoMatchingCases(console, requestedCases);
                return new TestCasesSearchFailure();
            }

            return new TestCasesSearchSuccess(selectedCases);
        }

        static TestCasesSearchResult TryGetAllCases(DirectoryInfo casesDir, ConsoleWriter console)
        {
            if (!casesDir.Exists)
            {
                PrintCasesDirNotFound(console, casesDir.FullName);
                return new TestCasesSearchFailure();
            }

            IReadOnlyList<TestCase> allCases = CaseDiscovery.DiscoverCases(casesDir);
            if (allCases.Count == 0)
            {
                PrintNoCasesFound(console, casesDir.FullName);
                return new TestCasesSearchFailure();
            }

            return new TestCasesSearchSuccess(allCases);
        }
    }

    abstract record TestCasesSearchResult;
    record TestCasesSearchSuccess(IReadOnlyList<TestCase> TestCases) : TestCasesSearchResult;
    record TestCasesSearchFailure : TestCasesSearchResult;

    static int CompareNodes<TCase>(
        IEnumerable<TCase> cases,
        Func<TCase, (JsonNode? Baseline, JsonNode? Target)> getNodes,
        string baselineLabel,
        string targetLabel,
        DiffAnalysisFormatter formatter,
        ProcessLogger logger)
        where TCase : TestCase
    {
        using IEnumerator<TCase> caseEnumerator = cases.GetEnumerator();
        if (!caseEnumerator.MoveNext())
        {
            logger.Warning("No test cases specified.");
            return 0;
        }

        int totalCases = 0, passed = 0, failed = 0, totalDiffs = 0;

        do
        {
            totalCases++;
            TCase caseInfo = caseEnumerator.Current;

            formatter.FormatCaseStart(caseInfo.Name, baselineLabel, targetLabel);

            int diffs = 0;
            try
            {
                (JsonNode? baselineNode, JsonNode? targetNode) = getNodes(caseInfo);

                if (targetNode is null || baselineNode is null)
                {
                    formatter.FormatCaseError(caseInfo.Name, "Failed to produce JSON node for comparison.");
                    failed++;
                    continue;
                }

                DiffReport diffReport = SemanticTreeDiffer.Compare(baselineNode, targetNode, caseInfo.Name);
                formatter.FormatCaseResult(caseInfo.Name, diffReport, baselineLabel, targetLabel);
                diffs = diffReport.Differences.Count;
            }
            catch (Exception ex)
            {
                failed++;
                formatter.FormatCaseError(caseInfo.Name, ex.Message);
                logger.Detail($"Exception details for '{caseInfo.Name}': {ex}");
                continue;
            }

            if (diffs == 0)
            {
                passed++;
            }
            else
            {
                failed++;
                totalDiffs += diffs;
            }
        }
        while (caseEnumerator.MoveNext());

        formatter.FormatSummary(totalCases, passed, failed, totalDiffs);
        return failed == 0 ? 0 : 1;
    }

    static int CompareBackendToBackend(
        TargetBackend target,
        TargetBackend baseline,
        IEnumerable<TestCase> cases,
        DiffAnalysisFormatter formatter,
        ProcessLogger logger)
    {
        if (target == baseline)
        {
            logger.Error($"Cannot compare backend '{target}' against itself.");
            return 1;
        }

        return CompareNodes(
            cases,
            testCase =>
            {
                using CompilationResult comp = CompilationHarness.Compile(ReadSourceCode(testCase), testCase.Name);
                return (GetTargetNode(baseline, comp), GetTargetNode(target, comp));
            },
            baseline.ToString().ToLowerInvariant(),
            target.ToString().ToLowerInvariant(),
            formatter,
            logger);
    }

    static int CompareBackendToExpected(
        TargetBackend backend,
        IEnumerable<VerifiedTestCase> cases,
        DiffAnalysisFormatter formatter,
        ProcessLogger logger)
    {
        return CompareNodes(
            cases,
            testCase =>
            {
                using CompilationResult comp = CompilationHarness.Compile(ReadSourceCode(testCase), testCase.Name);
                return (GetExpectedNode(testCase), GetTargetNode(backend, comp));
            },
            "expected",
            backend.ToString().ToLowerInvariant(),
            formatter,
            logger);
    }

    static JsonNode? GetExpectedNode(VerifiedTestCase testCase)
    {
        using FileStream stream = testCase.SnapshotFile.OpenRead();
        return JsonNode.Parse(stream);
    }

    enum TargetBackend
    {
        Roslyn,
        Reflection,
    }

    static JsonNode? GetTargetNode(TargetBackend backend, CompilationResult compResult)
    {
        switch (backend)
        {
            case TargetBackend.Roslyn:
                if (compResult.RoslynTopLevelTypes.Count == 1)
                    return DeclaredTypeTreeSerializer.Serialize(RectaleafRoslyn.FromSymbol(compResult.RoslynTopLevelTypes[0]));

                return new JsonArray([.. compResult.RoslynTopLevelTypes
                        .Select(t => DeclaredTypeTreeSerializer.Serialize(RectaleafRoslyn.FromSymbol(t)))]);

            case TargetBackend.Reflection:
                if (compResult.ReflectionTopLevelTypes.Count == 1)
                    return DeclaredTypeTreeSerializer.Serialize(RectaleafReflection.FromDeclaredType(compResult.ReflectionTopLevelTypes[0]));

                return new JsonArray([.. compResult.ReflectionTopLevelTypes
                        .Select(t => DeclaredTypeTreeSerializer.Serialize(RectaleafReflection.FromDeclaredType(t)))]);

            default:
                throw new InvalidOperationException($"Unknown target '{backend}'.");
        }
    }

    static int UpdateSnapshots(TargetBackend backend, DirectoryInfo outputDirectory, IEnumerable<TestCase> cases, ProcessLogger logger)
    {
        using IEnumerator<TestCase> caseEnumerator = cases.GetEnumerator();

        if (!caseEnumerator.MoveNext())
        {
            logger.Error("No test cases specified.");
            return 1;
        }

        if (!outputDirectory.Exists)
            outputDirectory.Create();

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        int updatedCount = 0;
        do
        {
            TestCase testCase = caseEnumerator.Current;
            try
            {
                using CompilationResult compResult = CompilationHarness.Compile(ReadSourceCode(testCase), testCase.Name);
                JsonNode? targetNode = GetTargetNode(backend, compResult);

                if (targetNode is null)
                {
                    logger.Error($"Failed updating snapshot for '{testCase.Name}': target backend '{backend.ToString().ToLowerInvariant()}' produced null node.");
                    continue;
                }

                string snapshotPath = Path.Combine(outputDirectory.FullName, $"{testCase.Name}.expected.json");
                string json = targetNode.ToJsonString(options);
                File.WriteAllText(snapshotPath, json);

                logger.Info($"  [UPDATED] Snapshot saved to: {snapshotPath}");
                updatedCount++;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed updating snapshot for '{testCase.Name}': {ex.Message}");
            }
        }
        while (caseEnumerator.MoveNext());

        logger.Info($"\nUpdated {updatedCount} snapshot file(s) in '{outputDirectory.FullName}'.");
        return 0;
    }

    static string ReadSourceCode(TestCase testCase) => testCase switch
    {
        VerifiedTestCase v => File.ReadAllText(v.CodeFile.FullName),
        NotVerifiedTestCase nv => File.ReadAllText(nv.CodeFile.FullName),
        _ => throw new InvalidOperationException($"Unknown test case type: {testCase.GetType()}")
    };
}
