namespace Rectaleaf.Tests.Infrastructure;

public sealed class ProcessLogger
{
    private readonly ConsoleWriter console;
    private readonly OutputVerbosity verbosity;

    public ProcessLogger(ConsoleWriter console, OutputVerbosity verbosity = OutputVerbosity.Normal)
    {
        this.console = console;
        this.verbosity = verbosity;
    }

    public void Error(string message)
    {
        console.WriteErrorLine($"[ERROR] {message}", ConsoleColor.Red);
    }

    public void Warning(string message)
    {
        if (verbosity >= OutputVerbosity.Normal)
            console.WriteLine($"[WARN] {message}", ConsoleColor.Yellow);
    }

    public void Info(string message)
    {
        if (verbosity >= OutputVerbosity.Normal)
            console.WriteLine(message);
    }

    public void Detail(string message)
    {
        if (verbosity >= OutputVerbosity.Detailed)
            console.WriteLine(message, ConsoleColor.DarkGray);
    }
}
