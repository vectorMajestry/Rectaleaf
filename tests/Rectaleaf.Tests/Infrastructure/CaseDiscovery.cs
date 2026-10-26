namespace Rectaleaf.Tests.Infrastructure;

public static class CaseDiscovery
{
    public static IReadOnlyList<TestCase> DiscoverCases(DirectoryInfo casesDirectory)
    {
        ArgumentNullException.ThrowIfNull(casesDirectory, nameof(casesDirectory));

        if (!casesDirectory.Exists)
            throw new DirectoryNotFoundException($"Test cases directory not found at '{casesDirectory.FullName}'.");

        var codeFiles = casesDirectory
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(f => Path.GetFileNameWithoutExtension(f.Name), f => f, StringComparer.OrdinalIgnoreCase);

        var snapshotFiles = casesDirectory
            .EnumerateFiles("*.expected.json", SearchOption.AllDirectories)
            .ToDictionary(f => f.Name[..^".expected.json".Length], f => f, StringComparer.OrdinalIgnoreCase);

        List<TestCase> testCases = new(codeFiles.Count);
        foreach ((string caseName, FileInfo codeFileInfo) in codeFiles)
        {
            if (snapshotFiles.TryGetValue(caseName, out FileInfo? snapshotFileInfo))
                testCases.Add(new VerifiedTestCase(caseName, codeFileInfo, snapshotFileInfo));
            else
                testCases.Add(new NotVerifiedTestCase(caseName, codeFileInfo));
        }
        return testCases;
    }

    public static TestCase? DiscoverCase(DirectoryInfo casesDirectory, string caseName)
    {
        ArgumentNullException.ThrowIfNull(casesDirectory, nameof(casesDirectory));
        ArgumentException.ThrowIfNullOrWhiteSpace(caseName, nameof(caseName));
        if (!casesDirectory.Exists)
            throw new DirectoryNotFoundException($"Test cases directory not found at '{casesDirectory.FullName}'.");

        FileInfo codeFile = new(Path.Combine(casesDirectory.FullName, $"{caseName}.cs"));
        if (!codeFile.Exists)
            return null;

        FileInfo snapshotFile = new(Path.Combine(casesDirectory.FullName, $"{caseName}.expected.json"));
        if (snapshotFile.Exists)
            return new VerifiedTestCase(caseName, codeFile, snapshotFile);
        else
            return new NotVerifiedTestCase(caseName, codeFile);
    }
}
