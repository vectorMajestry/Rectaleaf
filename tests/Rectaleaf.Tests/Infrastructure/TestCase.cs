namespace Rectaleaf.Tests.Infrastructure;

public abstract class TestCase
{
    public string Name { get; }

    public TestCase(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name;
    }
}

public sealed class NotVerifiedTestCase : TestCase
{
    public FileInfo CodeFile { get; }

    public NotVerifiedTestCase(string name, FileInfo codeFile) : base(name)
    {
        ArgumentNullException.ThrowIfNull(codeFile, nameof(codeFile));
        CodeFile = codeFile;
    }
}
public sealed class VerifiedTestCase : TestCase
{
    public FileInfo CodeFile { get; }
    public FileInfo SnapshotFile { get; }

    public VerifiedTestCase(string name, FileInfo codeFile, FileInfo snapshot) : base(name)
    {
        ArgumentNullException.ThrowIfNull(codeFile, nameof(codeFile));
        ArgumentNullException.ThrowIfNull(snapshot, nameof(snapshot));
        CodeFile = codeFile;
        SnapshotFile = snapshot;
    }
}