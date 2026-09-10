namespace TestCases;

using System;

public struct ModPoint
{
    public int X;
    public int Y;

    public ModPoint(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class ModifiersAndNativeContainer
{
    // Volatile fields (modopt IsVolatile)
    public volatile int VolatileState;
    public volatile bool VolatileFlag;

    // Readonly fields
    public readonly int ReadOnlyField = 42;
    public static readonly string StaticReadOnlyString = "Immutable";

    // Native integers (encoded as IntPtr/UIntPtr with NativeIntegerAttribute)
    public nint NativeSignedInt;
    public nuint NativeUnsignedInt;
    public nint? NullableNativeInt;
    public nint[] NativeIntArray = Array.Empty<nint>();
    public (nint first, nuint second) NativeIntTuple;

    private int _backingStorage;

    // By-ref and by-ref readonly return methods
    public ref int ReturnByRef() => ref _backingStorage;

    public ref readonly int ReturnByRefReadOnly() => ref _backingStorage;

    // Method parameter modifiers: in, ref readonly, ref, out
    public void ProcessParameters(
        in ModPoint inPoint,
        ref readonly ModPoint refReadOnlyPoint,
        ref ModPoint refPoint,
        out ModPoint outPoint)
    {
        outPoint = new ModPoint(inPoint.X + refPoint.X, refReadOnlyPoint.Y + refPoint.Y);
        refPoint.X += 1;
    }

    // Parameter scoping modifiers: scoped ref, scoped in
    public void ProcessScopedParameters(
        scoped ref ModPoint scopedRefPoint,
        scoped in ModPoint scopedInPoint)
    {
        scopedRefPoint.X += scopedInPoint.X;
    }

    // Method combining native integers, volatile access, and scoped references
    public nint ComputeWithNative(
        scoped in ModPoint point,
        nint multiplier,
        out nuint absoluteMagnitude)
    {
        nint calculated = (nint)point.X * multiplier;
        absoluteMagnitude = (nuint)Math.Abs(point.Y);
        return calculated;
    }
}
