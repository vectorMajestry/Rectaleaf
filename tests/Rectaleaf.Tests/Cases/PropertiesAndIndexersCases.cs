namespace TestCases;

using System;
using System.Runtime.CompilerServices;

public class PropertyAndIndexerContainer
{
    private int _backingInt;
    private static int _staticBackingInt;

    // Init-only auto property
    public int InitOnlyProp { get; init; } = 42;

    // Asymmetric accessibility combinations
    public string PublicGetPrivateSet { get; private set; } = "initial";

    public double PublicGetInternalInit { get; internal init; } = 3.14;

    public int PublicGetProtectedSet { get; protected set; } = 10;

    public long ProtectedGetPrivateProtectedSet { get; private protected set; } = 100L;

    // By-ref property return
    public ref int ByRefProperty => ref _backingInt;

    // By-ref readonly property return
    public ref readonly int ByRefReadOnlyProperty => ref _backingInt;

    // Static by-ref return property
    public static ref int StaticByRefProperty => ref _staticBackingInt;

    // Custom named indexer using IndexerNameAttribute
    [IndexerName("Grid")]
    public int this[int row, int col]
    {
        get => row * 100 + col;
        set => _backingInt = value;
    }

    // Overloaded indexer on the same type sharing the custom name
    [IndexerName("Grid")]
    public int this[string cellName]
    {
        get => cellName.Length;
        set => _backingInt = value;
    }

    // Standard field-like event
    public event Action? StandardEvent;

    // Custom event with explicit add and remove accessors
    private EventHandler? _customHandler;

    public event EventHandler CustomEvent
    {
        add
        {
            _customHandler += value;
        }
        remove
        {
            _customHandler -= value;
        }
    }

    public void TriggerEvents()
    {
        StandardEvent?.Invoke();
        _customHandler?.Invoke(this, EventArgs.Empty);
    }
}

// Indexer with optional parameters and custom name
public class OptionalParamIndexerContainer
{
    private readonly double[] _values = new double[100];

    [IndexerName("Measurements")]
    public double this[int index, int scale = 1, double offset = 0.0]
    {
        get => _values[index] * scale + offset;
        set => _values[index] = (value - offset) / scale;
    }
}
