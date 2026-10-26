namespace Rectaleaf.Tests.Infrastructure;

public sealed class ConsoleWriter
{
    public void Write(string text, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
            Console.Write(text);
            Console.ResetColor();
        }
        else
        {
            Console.Write(text);
        }
    }

    public void WriteLine(string text = "", ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine(text);
        }
    }

    public void WriteErrorLine(string text, ConsoleColor? color = ConsoleColor.Red)
    {
        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
            Console.Error.WriteLine(text);
            Console.ResetColor();
        }
        else
        {
            Console.Error.WriteLine(text);
        }
    }

    public string? ReadLine() => Console.ReadLine();
}
