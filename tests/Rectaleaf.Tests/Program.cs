using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Reflection;
using Rectaleaf.Roslyn;
using Rectaleaf.Tests.Infrastructure;

namespace Rectaleaf.Tests;

public static class Program
{
    public static int Main(string[] args)
    {
        ConsoleReporter.PrintHeader("Rectaleaf Test Runner");

        if (args.Length == 0)
        {
            PrintUsageError("No command specified.");
            return 1;
        }

        // 1. Synthetic test flag
        if (args.Any(a => a.Equals("--synthetic-test", StringComparison.OrdinalIgnoreCase)))
        {
            return RunSyntheticTest();
        }

        // 2. Snapshot update flag
        int updateIndex = Array.FindIndex(args, a => a.Equals("--update-snapshots", StringComparison.OrdinalIgnoreCase));
        if (updateIndex >= 0)
        {
            if (updateIndex + 2 >= args.Length)
            {
                PrintUsageError("--update-snapshots requires <backend> and <filename.cs|all> arguments.\nExample: --update-snapshots roslyn MyCase.cs\nExample: --update-snapshots reflection all");
                return 1;
            }

            string backend = args[updateIndex + 1].ToLowerInvariant();
            string targetFile = args[updateIndex + 2];

            if (backend is not ("roslyn" or "reflection"))
            {
                PrintUsageError($"Invalid backend '{backend}'. Must be 'roslyn' or 'reflection'.");
                return 1;
            }

            return UpdateSnapshots(backend, targetFile);
        }

        // 3. Comparison flag
        int compareIndex = Array.FindIndex(args, a => a.Equals("--compare", StringComparison.OrdinalIgnoreCase));
        if (compareIndex >= 0)
        {
            if (compareIndex + 2 >= args.Length)
            {
                PrintUsageError("--compare requires two targets.\nExamples:\n  --compare roslyn expected\n  --compare reflection expected\n  --compare roslyn reflection");
                return 1;
            }

            string targetA = args[compareIndex + 1].ToLowerInvariant();
            string targetB = args[compareIndex + 2].ToLowerInvariant();

            string? caseFilter = null;
            int caseFilterIndex = Array.FindIndex(args, a => a.Equals("--case", StringComparison.OrdinalIgnoreCase));
            if (caseFilterIndex >= 0 && caseFilterIndex + 1 < args.Length)
            {
                caseFilter = args[caseFilterIndex + 1];
            }

            return RunComparison(targetA, targetB, caseFilter);
        }

        PrintUsageError($"Unrecognized arguments: {string.Join(" ", args)}");
        return 1;
    }

    static void PrintUsageError(string error)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {error}\n");
        Console.ResetColor();

        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tests/Rectaleaf.Tests -- --compare <source> <target> [--case <name>]");
        Console.WriteLine("    Valid comparison combinations:");
        Console.WriteLine("      --compare roslyn expected");
        Console.WriteLine("      --compare reflection expected");
        Console.WriteLine("      --compare roslyn reflection");
        Console.WriteLine();
        Console.WriteLine("  dotnet run --project tests/Rectaleaf.Tests -- --update-snapshots <roslyn|reflection> <filename.cs|all>");
        Console.WriteLine();
        Console.WriteLine("  dotnet run --project tests/Rectaleaf.Tests -- --synthetic-test");
    }

    static int RunSyntheticTest()
    {
        ConsoleReporter.PrintHeader("Running Synthetic Tree Differ Verification Test");

        // Construct Expected node tree
        var expectedTree = new JsonObject
        {
            ["kind"] = "Struct",
            ["localName"] = "MyTypeName",
            ["nestedTypes"] = new JsonArray
            {
                new JsonObject
                {
                    ["kind"] = "Struct",
                    ["localName"] = "yNestedType",
                    ["methods"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "MyMethod",
                            ["returnInfo"] = new JsonObject
                            {
                                ["type"] = new JsonObject { ["localName"] = "int" }
                            },
                            ["localFunctions"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["name"] = "MyLocalFuncA",
                                    ["localFunctions"] = new JsonArray
                                    {
                                        new JsonObject
                                        {
                                            ["name"] = "MyLocalFuncB",
                                            ["parameters"] = new JsonArray
                                            {
                                                new JsonObject
                                                {
                                                    ["name"] = "paramName",
                                                    ["position"] = 3,
                                                    ["modifiers"] = "scoped ref",
                                                    ["type"] = new JsonObject { ["localName"] = "int" }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Construct Actual node tree with intentional discrepancies:
        // 1. Parameter 3 modifiers: "ref" instead of "scoped ref"
        // 2. MyMethod return type: "string" instead of "int"
        // 3. Unexpected property: "extraDebugField" on root
        var actualTree = new JsonObject
        {
            ["kind"] = "Struct",
            ["localName"] = "MyTypeName",
            ["extraDebugField"] = "unexpected",
            ["nestedTypes"] = new JsonArray
            {
                new JsonObject
                {
                    ["kind"] = "Struct",
                    ["localName"] = "yNestedType",
                    ["methods"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "MyMethod",
                            ["returnInfo"] = new JsonObject
                            {
                                ["type"] = new JsonObject { ["localName"] = "string" }
                            },
                            ["localFunctions"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["name"] = "MyLocalFuncA",
                                    ["localFunctions"] = new JsonArray
                                    {
                                        new JsonObject
                                        {
                                            ["name"] = "MyLocalFuncB",
                                            ["parameters"] = new JsonArray
                                            {
                                                new JsonObject
                                                {
                                                    ["name"] = "paramName",
                                                    ["position"] = 3,
                                                    ["modifiers"] = "ref",
                                                    ["type"] = new JsonObject { ["localName"] = "int" }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var report = SemanticTreeDiffer.Compare(expectedTree, actualTree, "MyTypeName");

        ConsoleReporter.PrintDifferences(report.Differences);

        bool hasParamMismatch = report.Differences.Any(d =>
            d.Message.Contains("Parameter 3 of MyTypeName.yNestedType.MyMethod.MyLocalFuncA.MyLocalFuncB has to be 'scoped ref int paramName'; got 'ref int paramName'."));

        bool hasReturnTypeMismatch = report.Differences.Any(d =>
            d.Path.Contains("returnInfo") && d.Actual == "string");

        bool hasExtraProperty = report.Differences.Any(d =>
            d.Path.Contains("extraDebugField"));

        if (hasParamMismatch && hasReturnTypeMismatch && hasExtraProperty)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[SYNTHETIC TEST VERIFIED] SemanticTreeDiffer correctly detected and formatted all discrepancies with exact breadcrumbs.");
            Console.ResetColor();
            return 0;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n[SYNTHETIC TEST FAILED] One or more expected discrepancies were not formatted as expected.");
        Console.ResetColor();
        return 1;
    }

    static int RunComparison(string targetA, string targetB, string? caseFilter)
    {
        var validTargets = new HashSet<string> { "roslyn", "reflection", "expected" };
        if (!validTargets.Contains(targetA) || !validTargets.Contains(targetB) || targetA == targetB)
        {
            PrintUsageError($"Invalid comparison pair '{targetA}' vs '{targetB}'. Choose two distinct targets from: roslyn, reflection, expected.");
            return 1;
        }

        var cases = CaseDiscovery.DiscoverCases();
        if (!string.IsNullOrEmpty(caseFilter))
        {
            cases = cases.Where(c => c.Name.Contains(caseFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (cases.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"No test cases found in assembly embedded resources{(caseFilter != null ? $" matching filter '{caseFilter}'" : "")}.");
            Console.ResetColor();
            return 0;
        }

        int passed = 0;
        int failed = 0;
        int totalDiffs = 0;

        foreach (var testCase in cases)
        {
            Console.WriteLine($"\nRunning Case: {testCase.Name} (Comparing {targetA} vs {targetB})...");

            try
            {
                using var compResult = CompilationHarness.Compile(testCase.SourceCode, testCase.Name);

                JsonNode? nodeA = GetTargetNode(targetA, testCase, compResult);
                JsonNode? nodeB = GetTargetNode(targetB, testCase, compResult);

                if (nodeA == null || nodeB == null)
                {
                    ConsoleReporter.PrintCaseFailure(testCase.Name, 1);
                    failed++;
                    continue;
                }

                // The second argument (targetB) is the correct baseline; the first argument (targetA) is compared against it.
                var diffReport = SemanticTreeDiffer.Compare(nodeB, nodeA, testCase.Name);
                if (diffReport.HasDifferences)
                {
                    failed++;
                    totalDiffs += diffReport.Differences.Count;
                    ConsoleReporter.PrintCaseFailure(testCase.Name, diffReport.Differences.Count);
                    ConsoleReporter.PrintDifferences(diffReport.Differences, targetB, targetA);
                }
                else
                {
                    passed++;
                    ConsoleReporter.PrintCaseSuccess(testCase.Name);
                }
            }
            catch (Exception ex)
            {
                failed++;
                ConsoleReporter.PrintCaseFailure(testCase.Name, 1);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"      Exception: {ex.Message}");
                Console.ResetColor();
            }
        }

        ConsoleReporter.PrintSummary(cases.Count, passed, failed, totalDiffs);
        return failed == 0 ? 0 : 1;
    }

    static JsonNode? GetTargetNode(string target, TestCase testCase, CompilationResult compResult)
    {
        switch (target)
        {
            case "expected":
                if (string.IsNullOrWhiteSpace(testCase.ExpectedJson))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"      Missing expected snapshot for '{testCase.Name}'. Run with --update-snapshots to create it.");
                    Console.ResetColor();
                    return null;
                }
                return JsonNode.Parse(testCase.ExpectedJson);

            case "roslyn":
                var roslynRoots = new JsonArray();
                foreach (var sym in compResult.RoslynTopLevelTypes)
                {
                    var declaredType = RectaleafRoslyn.FromSymbol(sym);
                    roslynRoots.Add(DeclaredTypeTreeSerializer.Serialize(declaredType));
                }
                return roslynRoots.Count == 1 ? roslynRoots[0] : roslynRoots;

            case "reflection":
                var reflRoots = new JsonArray();
                foreach (var t in compResult.ReflectionTopLevelTypes)
                {
                    var declaredType = RectaleafReflection.FromDeclaredType(t);
                    reflRoots.Add(DeclaredTypeTreeSerializer.Serialize(declaredType));
                }
                return reflRoots.Count == 1 ? reflRoots[0] : reflRoots;

            default:
                throw new InvalidOperationException($"Unknown target '{target}'.");
        }
    }

    static int UpdateSnapshots(string backend, string targetFile)
    {
        var cases = CaseDiscovery.DiscoverCases();

        if (targetFile.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"Are you sure you want to update ALL {cases.Count} snapshot(s) using backend '{backend}'? (Y/N): ");
            Console.ResetColor();

            var response = Console.ReadLine()?.Trim();
            if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Snapshot update aborted by user.");
                return 0;
            }
        }
        else
        {
            cases = cases.Where(c => c.Name.Equals(targetFile, StringComparison.OrdinalIgnoreCase)
                || c.Name.Equals(Path.GetFileName(targetFile), StringComparison.OrdinalIgnoreCase)).ToList();

            if (cases.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"No test case found matching '{targetFile}'.");
                Console.ResetColor();
                return 1;
            }
        }

        int updatedCount = 0;
        foreach (var testCase in cases)
        {
            if (string.IsNullOrEmpty(testCase.DiskExpectedFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Cannot update snapshot for '{testCase.Name}': disk destination path could not be determined.");
                Console.ResetColor();
                continue;
            }

            try
            {
                using var compResult = CompilationHarness.Compile(testCase.SourceCode, testCase.Name);
                var targetNode = GetTargetNode(backend, testCase, compResult);

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    if (targetNode == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  [FAILED] Failed updating snapshot for '{testCase.Name}': null target node");
                        Console.ResetColor();
                        continue;
                    }
                    string json = targetNode.ToJsonString(options);
                    File.WriteAllText(testCase.DiskExpectedFilePath, json);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  [UPDATED] Snapshot saved to: {testCase.DiskExpectedFilePath}");
                    Console.ResetColor();
                    updatedCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAILED] Failed updating snapshot for '{testCase.Name}': {ex.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine($"\nUpdated {updatedCount} snapshot file(s).");
        return 0;
    }
}