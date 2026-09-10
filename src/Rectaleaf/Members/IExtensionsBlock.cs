using Rectaleaf.Components;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.Members;

/// <summary>
/// Represents "extension members" block.
/// </summary>
/// <remarks>
/// This interface should not be implemented directly.
/// Implement <see cref="IInstanceExtensionsBlock"/> or <see cref="IStaticExtensionsBlock"/> instead.
/// </remarks>
public interface IExtensionsBlock
{
    /// <summary>
    /// Gets the static class type that declares this extension block.
    /// </summary>
    IStaticClassType DeclaringType { get; }

    /// <summary>
    /// Gets the target type being extended.
    /// </summary>
    ITypeUsage<IKnownOrNot> ExtendedType { get; }

    /// <summary>
    /// Gets a value indicating whether this extension block declares type parameters.
    /// </summary>
    bool IsGeneric { get; }

    /// <summary>
    /// Gets the type parameters declared directly on this extension block.
    /// </summary>
    IReadOnlyList<IGenericParameter> GetGenericParameters();

    /// <summary>
    /// Gets the attributes applied to this extension block.
    /// </summary>
    IReadOnlyList<IAttribute> GetAttributes();

    /// <summary>
    /// Gets all extension methods declared within this block.
    /// </summary>
    IReadOnlyList<IMethod> GetMethods();

    /// <summary>
    /// Gets all extension properties declared within this block.
    /// </summary>
    IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties();

    /// <summary>
    /// Gets all extension indexers declared within this block.
    /// </summary>
    IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers();
}

/// <summary>
/// Represents an extension block with a named receiver parameter
/// (e.g. <c>extension(ref Point pt)</c> or <c>extension&lt;T&gt;(List&lt;T&gt; list)</c>).
/// Can contain both instance and static extension members.
/// </summary>
public interface IInstanceExtensionsBlock : IExtensionsBlock
{
    /// <summary>
    /// Gets the full receiver parameter declaration (including parameter name, type usage, and modifiers such as <c>ref</c>, <c>in</c>, or <c>ref readonly</c>).
    /// </summary>
    IParameter Receiver { get; }
}

/// <summary>
/// Represents a static extension block with an unnamed receiver 
/// (e.g. <c>extension(string)</c> or <c>extension&lt;T&gt;(List&lt;T&gt;)</c>).
/// Can only contain static extension members.
/// </summary>
public interface IStaticExtensionsBlock : IExtensionsBlock;
