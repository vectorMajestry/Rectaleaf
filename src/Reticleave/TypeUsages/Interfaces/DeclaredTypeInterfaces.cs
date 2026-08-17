using Reticleave.Metadata.Members;
using Reticleave.Components;
using Reticleave.Members;

namespace Reticleave.Metadata.TypeUsages.Interfaces;

/// <summary>
/// Represents a declared type definition (e.g., class, struct, record, interface, enum, delegate).
/// </summary>
public interface IDeclaredType
{
    string FullyQualifiedName { get; }
    string LocalName { get; }
    string? Namespace { get; }
    IHasNestedTypes? ContainingType { get; }
    IReadOnlyList<IAttribute> GetAttributes();
    AccessModifier AccessModifier { get; }
}

/// <summary>
/// Represents a value type definition (`<c>where T : struct</c>` / value semantics).
/// </summary>
public interface IValueType : IDeclaredType;

/// <summary>
/// Represents a reference type definition (`<c>where T : class</c>` / reference semantics).
/// </summary>
public interface IReferenceType : IDeclaredType;

/// <summary>
/// Represents a standard struct definition. Not a ref struct.
/// </summary>
public interface IStructType : IValueType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    bool IsReadOnly { get; }
}

/// <summary>
/// Represents a ref struct definition.
/// </summary>
public interface IRefStructType : IValueType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    bool IsReadOnly { get; }
}

/// <summary>
/// Represents a record struct definition.
/// </summary>
public interface IRecordStructType : IValueType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    bool IsReadOnly { get; }
}

/// <summary>
/// Represents an inline array struct definition (decorated with <see cref="System.Runtime.CompilerServices.InlineArrayAttribute"/>).
/// </summary>
public interface IInlineArrayType : IValueType, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint, IHasSize
{
    /// <summary>
    /// Gets the element type of this inline array.
    /// </summary>
    IComposableType<IKnownOrNot> ElementType { get; }

    /// <summary>
    /// Gets the fixed length of the inline array.
    /// </summary>
    int Length { get; }

    bool IsReadOnly { get; }
}

/// <summary>
/// Represents an enum definition.
/// </summary>
public interface IEnumType : IValueType, IHasSize, IHasNestedTypes, IValidConstraint
{
    IReadOnlyList<IEnumMember> Members { get; }
    IEnumUnderlyingType GetUnderlyingType();
}

/// <summary>
/// Represents an enum definition with a known underlying integer representation.
/// </summary>
/// <typeparam name="T">
/// The underlying type of the enum members. 
/// Corresponds to one of '<see cref="EnumUnderlyingTypeEnum"/>' members.
/// </typeparam>
public interface IEnumType<T> : IEnumType
    where T : struct
{
    new IReadOnlyList<IEnumMember<T>> Members { get; }
}

/// <summary>
/// Represents a class definition (instantiable or abstract).
/// </summary>
public interface IClassType : IReferenceType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    IDeclTypeUsage<IKnownOrNot, IClassType>? BaseType { get; }
}

/// <summary>
/// Represents an instantiable class definition (non-abstract, non-static).
/// </summary>
public interface IRegularClassType : IClassType
{
    bool IsSealed { get; }
}

/// <summary>
/// Represents an abstract class definition.
/// </summary>
public interface IAbstractClassType : IClassType;

/// <summary>
/// Represents a record class definition.
/// </summary>
public interface IRecordClassType : IReferenceType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    IDeclTypeUsage<IKnownOrNot, IRecordClassType>? BaseType { get; }
    bool IsSealed { get; }
}

/// <summary>
/// Represents a static class definition.
/// </summary>
public interface IStaticClassType : IReferenceType, IHasFields, IHasMethods, IHasProperties,
    IHasEvents, IHasGenericParameters, IHasStaticConstructor, IHasNestedTypes, IHasOperators;

/// <summary>
/// Represents an interface definition.
/// </summary>
public interface IInterfaceType : IReferenceType, IHasMethods, IHasProperties, IHasIndexers, IHasEvents,
    IHasGenericParameters, IHasStaticConstructor, IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint;

/// <summary>
/// Represents a delegate definition.
/// </summary>
public interface IDelegateType : IReferenceType, IHasGenericParameters, IHasNestedTypes, IValidConstraint
{
    IReturnInfo<IKnownOrNot> ReturnInfo { get; }
    IReadOnlyList<IParameter> GetParameters();
}
