namespace TestCases;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Inline array struct with fixed element length
[InlineArray(16)]
public struct InlineByteArray16
{
    private byte _element0;
}

[InlineArray(4)]
public struct InlineIntArray4
{
    private int _element0;
}

// Basic ref struct
public ref struct PlainRefStruct
{
    public int Value;
    public Span<byte> DataSpan;

    public PlainRefStruct(int value, Span<byte> data)
    {
        Value = value;
        DataSpan = data;
    }
}

// Readonly ref struct with ref fields and ref readonly fields
public readonly ref struct ReadOnlyRefStructWithRefFields
{
    public readonly ref int MutableTarget;
    public readonly ref readonly int ReadOnlyTarget;

    public ReadOnlyRefStructWithRefFields(ref int mutableTarget, ref readonly int readOnlyTarget)
    {
        MutableTarget = ref mutableTarget;
        ReadOnlyTarget = ref readOnlyTarget;
    }
}

// Ref struct implementing interfaces (supported in modern .NET)
public ref struct InterfaceImplementingRefStruct : IDisposable, IEquatable<InterfaceImplementingRefStruct>
{
    public int State;

    public void Dispose()
    {
        State = 0;
    }

    public bool Equals(InterfaceImplementingRefStruct other)
    {
        return State == other.State;
    }
}

// Unsafe struct with multiple fixed-size buffer fields
public unsafe struct FixedBufferContainer
{
    public fixed int IntBuffer[32];
    public fixed byte ByteBuffer[64];
    public fixed char CharBuffer[128];
    public fixed long LongBuffer[8];

    public int RegularField;
}

// Explicit memory layout struct with overlapping fields (unions)
[StructLayout(LayoutKind.Explicit)]
public struct OverlappingUnionStruct
{
    [FieldOffset(0)]
    public int IntValue;

    [FieldOffset(0)]
    public float FloatValue;

    [FieldOffset(0)]
    public byte Byte0;

    [FieldOffset(1)]
    public byte Byte1;

    [FieldOffset(4)]
    public uint SecondaryValue;
}
