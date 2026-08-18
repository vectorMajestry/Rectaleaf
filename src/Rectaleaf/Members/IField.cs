using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.Members;


/// <summary>
/// Represents a field in a type.
/// </summary>
/// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IRefField{TKind, TMutability}"/>, <see cref="ConstField"/>,<see cref="FixedSizeBufferField"/>,
/// <see cref="IStaticField{TKind}"/>, or <see cref="IInstanceField{TKind}"/> instead.
/// </remarks>
public interface IField<out TKind>
    where TKind : IKnownOrNot
{
    string Name { get; }
    AccessModifier AccessModifier { get; }
    IHasFields DeclaringType { get; }
    IReadOnlyList<IAttribute> GetAttributes();
}

public interface IRefField<out TKind, out TMutability> : IField<TKind>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    IByRefType<TKind, TMutability> Type { get; }
}

public interface ConstField : IField<IKnown>
{
    IComposableType<IKnown> Type { get; }
    object GetConstantValue();
}

public interface FixedSizeBufferField : IField<IKnown>
{
    IFixedSizeBufferType Type { get; }
    int Length { get; }
}

/// <summary>
/// Represents a regular field (non-const, non-fixed-size buffer, non-ref) in a type.
/// </summary>.
/// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IStaticField{TKind}"/> or <see cref="IInstanceField{TKind}"/> instead.
/// </remarks>
/// <typeparam name="TKind"></typeparam>
public interface IRegularField<out TKind> : IField<TKind>
    where TKind : IKnownOrNot
{
    IComposableType<TKind> Type { get; }
    bool IsReadOnly { get; }
    bool IsVolatile { get; }
}

public interface IStaticField<out TKind> : IRegularField<TKind>
    where TKind : IKnownOrNot
{
}

public interface IInstanceField<out TKind> : IRegularField<TKind>
    where TKind : IKnownOrNot
{
    bool IsRequired { get; }
}
