using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rectaleaf.Tests.Infrastructure;

public sealed record TestCase(
    string Name,
    string SourceCode,
    string? ExpectedJson,
    string? DiskSourceFilePath,
    string? DiskExpectedFilePath
);

public static class CaseDiscovery
{
    public static List<TestCase> DiscoverCases()
    {
        var cases = new List<TestCase>();

        string? projectRoot = FindProjectDirectory();
        string? projectCasesDir = projectRoot is not null ? Path.Combine(projectRoot, "Cases") : null;
        string appCasesDir = Path.Combine(AppContext.BaseDirectory, "Cases");

        // Prefer project Cases directory during local development, fall back to app output directory
        string? effectiveCasesDir = null;
        if (projectCasesDir is not null && Directory.Exists(projectCasesDir))
            effectiveCasesDir = projectCasesDir;
        else if (Directory.Exists(appCasesDir))
            effectiveCasesDir = appCasesDir;

        if (effectiveCasesDir is not null && Directory.Exists(effectiveCasesDir))
        {
            var csFiles = Directory.GetFiles(effectiveCasesDir, "*.cs", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var csFile in csFiles)
            {
                string simpleName = Path.GetFileName(csFile);
                string relativePath = Path.GetRelativePath(effectiveCasesDir, csFile);
                string sourceCode = File.ReadAllText(csFile);

                string expectedFile = Path.ChangeExtension(csFile, ".expected.json");
                string? expectedJson = File.Exists(expectedFile) ? File.ReadAllText(expectedFile) : null;

                string diskSourcePath = projectCasesDir is not null
                    ? Path.Combine(projectCasesDir, relativePath)
                    : csFile;
                string diskExpectedPath = Path.ChangeExtension(diskSourcePath, ".expected.json");

                cases.Add(new TestCase(simpleName, sourceCode, expectedJson, diskSourcePath, diskExpectedPath));
            }

            if (cases.Count > 0)
                return cases;
        }

        // Fallback: check embedded resources if present
        var assembly = typeof(CaseDiscovery).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();
        var csResources = resourceNames
            .Where(r => r.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && r.Contains(".Cases."))
            .ToList();

        foreach (var csRes in csResources)
        {
            string sourceCode = ReadResource(assembly, csRes);
            string expectedRes = csRes.Substring(0, csRes.Length - 3) + ".expected.json";
            string? expectedJson = resourceNames.Contains(expectedRes) ? ReadResource(assembly, expectedRes) : null;

            string relativeName = csRes.Substring(csRes.IndexOf(".Cases.", StringComparison.Ordinal) + 7);
            string simpleName = Path.GetFileName(relativeName);

            string? diskSourcePath = null;
            string? diskExpectedPath = null;
            if (projectCasesDir is not null)
            {
                diskSourcePath = Path.Combine(projectCasesDir, relativeName.Replace('.', Path.DirectorySeparatorChar));
                diskSourcePath = diskSourcePath.Substring(0, diskSourcePath.LastIndexOf(Path.DirectorySeparatorChar)) + ".cs";
                diskExpectedPath = Path.ChangeExtension(diskSourcePath, ".expected.json");
            }

            cases.Add(new TestCase(simpleName, sourceCode, expectedJson, diskSourcePath, diskExpectedPath));
        }

        return cases;
    }

    static string ReadResource(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Resource '{name}' not found in assembly '{assembly.FullName}'.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string? FindProjectDirectory()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Rectaleaf.Tests.csproj")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}