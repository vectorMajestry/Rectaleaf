namespace TestCases;

using System;
using System.Text;

// Positional record class with synthesized boilerplate
public record SimplePerson(string FirstName, string LastName);

// Record class inheritance and base constructor forwarding
public record Employee(string FirstName, string LastName, int EmployeeId, string Department)
    : SimplePerson(FirstName, LastName)
{
    public DateTime HireDate { get; init; } = DateTime.UtcNow;

    // Explicit custom method coexisting with synthesized ones
    public string FullName => $"{FirstName} {LastName} (#{EmployeeId})";
}

// Record class with explicit PrintMembers override and custom properties
public record CustomPrintRecord(string Identifier, int Level)
{
    public string Description { get; set; } = string.Empty;

    // Custom PrintMembers to verify both backends distinguish explicit from synthesized
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"CustomId: {Identifier}, CustomLevel: {Level}");
        return true;
    }

    // Explicit Deconstruct overload
    public void Deconstruct(out string id, out int lvl, out string desc)
    {
        id = Identifier;
        lvl = Level;
        desc = Description;
    }
}

// Positional record struct
public record struct MutableRecordStruct(double X, double Y)
{
    public readonly double DistanceToOrigin() => Math.Sqrt(X * X + Y * Y);

    public void Scale(double factor)
    {
        X *= factor;
        Y *= factor;
    }
}

// Readonly record struct
public readonly record struct ReadOnlyRecordPoint(int X, int Y, int Z)
{
    public static ReadOnlyRecordPoint Origin => new(0, 0, 0);
}

// Generic record with type constraints
public record GenericRecordPair<TKey, TVal>(TKey Key, TVal Value)
    where TKey : notnull, IComparable<TKey>
    where TVal : class?
{
    public bool HasValue => Value is not null;
}
