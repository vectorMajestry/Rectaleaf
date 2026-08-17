using Reticleave.Components;

namespace Reticleave.Metadata.TypeUsages.Interfaces;

public interface IInnerType<out TKind> : IComposableType<TKind>
    where TKind : IKnownOrNot;

public interface IPredefinedType : IInnerType<IKnown>, IHasNullability
{
    PredefinedTypeKind Kind { get; }
}

public interface IFixedSizeBufferType : IInnerType<IKnown>
{
    FixedSizeBufferTypeEnum Kind { get; }
}

public interface IEnumUnderlyingType : IInnerType<IKnown>
{
    EnumUnderlyingTypeEnum Kind { get; }
}

/// <summary>
/// Represents any usage of any declared type (not a predefined type; may or may not have a type argument list). 
/// This is a shortcut interface to <see cref="IDeclTypeUsage{TKind, TDecl}"/> when TDecl is <see cref="IDeclaredType"/>.
/// </summary>
/// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IBoundGenericType{TKind, TDecl}"/> and <see cref="IPlainType{TDecl}"/> instead.
/// </remarks>
public interface IDeclTypeUsage<out TKind> : IInnerType<TKind>, IHasNullability
    where TKind : IKnownOrNot
{
    IDeclaredType TypeDefinition { get; }
}

/// <summary>
/// Represents any usage of a declared type (not a predefined type; may or may not have a type argument list).
/// </summary>
/// /// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IBoundGenericType{TKind, TDecl}"/> and <see cref="IPlainType{TDecl}"/> instead.
/// </remarks>
public interface IDeclTypeUsage<out TKind, out TDecl> : IDeclTypeUsage<TKind>
    where TKind : IKnownOrNot
    where TDecl : IDeclaredType
{
    new TDecl TypeDefinition { get; }
}

/// <summary>
/// Represents a non-generic declared type usage (e.g. 'TypeName' in `TypeName _item = new();`).
/// </summary>
public interface IPlainType<out TDecl> : IDeclTypeUsage<IKnown, TDecl>
    where TDecl : IDeclaredType;

/// <summary>
/// Represents a bound generic type usage (e.g. List&lt;int&gt;, KeyValuePair&lt;string, int&gt;).
/// </summary>
public interface IBoundGenericType<out TKind, out TDecl> : IDeclTypeUsage<TKind, TDecl>
    where TKind : IKnownOrNot
    where TDecl : IHasGenericParameters
{
    [Obsolete("Use 'TypeArguments' extension property instead.")]
    IReadOnlyList<IComposableType<IKnownOrNot>> TypeArgumentsMixed { get; }
}

/// <summary>
/// Represents a usage of a generic parameter.
/// </summary>
public interface IPlaceholderType : IInnerType<IIncomplete>, IHasNullability, IValidConstraint
{
    IGenericParameter Info { get; }
}

/// <summary>
/// Represents a function pointer type usage (e.g., `<![CDATA[delegate*<int, void>]]>` or `<![CDATA[delegate* unmanaged[Cdecl]<int, void>]]>`).
/// </summary>
public interface IFunctionPointerType<out TKind> : IInnerType<TKind>
    where TKind : IKnownOrNot
{
    ITypeUsage<TKind> ReturnType { get; }
    IReadOnlyList<IParameter> Parameters { get; }
}

/// <summary>
/// Represents a managed function pointer type usage (e.g., `<![CDATA[delegate*<int, void>]]>`).
/// </summary>
public interface IManagedFunctionPointerType<out TKind> : IFunctionPointerType<TKind>
    where TKind : IKnownOrNot;

/// <summary>
/// Represents an unmanaged function pointer type usage (e.g., `<![CDATA[delegate* unmanaged[Cdecl]<int, void>]]>`).
/// </summary>
public interface IUnmanagedFunctionPointerType<out TKind> : IFunctionPointerType<TKind>
    where TKind : IKnownOrNot
{
    /// <summary>
    /// Gets the list of calling convention types applied to this unmanaged function pointer 
    /// (e.g., <see cref="System.Runtime.CompilerServices.CallConvCdecl"/>).
    /// If calling conventions were omitted (`<![CDATA[delegate* unmanaged<...>]]>`), this list will be empty.
    /// </summary>
    IReadOnlyList<IDeclaredType> CallingConventions { get; }
}

/// <summary>
/// Represents an element of a rich tuple type usage containing its element type usage and an optional element name.
/// </summary>
public interface ITupleElement<out TKind>
    where TKind : IKnownOrNot
{
    /// <summary>
    /// Gets the type usage of this tuple element.
    /// </summary>
    IComposableType<TKind> Type { get; }

    /// <summary>
    /// Gets the explicit element name assigned to this tuple element (e.g., <c>"Code"</c> in 
    /// `<![CDATA[(int Code, string Message)]]>`), or <see langword="null"/> if no name was assigned.
    /// </summary>
    string? Name { get; }
}

/// <summary>
/// Represents a tuple type usage (e.g., `<![CDATA[(int Code, string Message)]]>` or `<![CDATA[ValueTuple<int, string>]]>`).
/// Both rich tuple syntax and explicit <see cref="System.ValueTuple"/> chain syntax map to this interface.
/// </summary>
public interface ITupleType<out TKind> : IInnerType<TKind>, IHasNullability
    where TKind : IKnownOrNot
{
    /// <summary>
    /// Gets the collection of types contained within this tuple.
    /// </summary>
    IReadOnlyList<IComposableType<TKind>> ElementTypes { get; }

    /// <summary>
    /// Gets the bound generic struct type describing the underlying <see cref="System.ValueTuple"/> chain.
    /// Can be queried even if the tuple was created via tuple syntax (e.g., `<![CDATA[(int Code, string Message)]]>`).
    /// </summary>
    IBoundGenericType<TKind, IStructType> UnderlyingValueTupleType { get; }
}

/// <summary>
/// Represents a rich tuple type created via tuple syntax (e.g., `<![CDATA[(int Code, string Message)]]>`).
/// </summary>
public interface IRichTupleType<out TKind> : ITupleType<TKind>
    where TKind : IKnownOrNot
{
    /// <summary>
    /// Gets the collection of tuple elements containing their types and names.
    /// </summary>
    IReadOnlyList<ITupleElement<TKind>> Elements { get; }
}

/// <summary>
/// Represents a tuple type usage created via explicit <see cref="System.ValueTuple"/> chain declaration syntax 
/// (e.g., `<![CDATA[ValueTuple<int, string>]]>`).
/// </summary>
public interface IChainTupleType<out TKind> : ITupleType<TKind>
    where TKind : IKnownOrNot;

/// <summary>
/// Represents a tuple type usage where the source analysis backend failed to determine whether explicit 
/// tuple syntax or manual <see cref="System.ValueTuple"/> chain was used.
/// </summary>
public interface IObliviousTupleType<out TKind> : ITupleType<TKind>
    where TKind : IKnownOrNot;

