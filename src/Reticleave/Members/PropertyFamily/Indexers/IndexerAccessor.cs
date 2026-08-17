using Reticleave.Metadata.Members.PropertyFamily;
using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;

namespace Reticleave.Metadata.Members.PropertyFamily.Indexers;

/// <summary>
/// Represents an indexer accessor.
/// </summary>
/// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IIndexerGetAccessor{TKind}"/>, <see cref="IIndexerSetAccessor{TKind}"/>, 
/// or <see cref="IIndexerInitAccessor{TKind}"/> instead.
/// </remarks>
public interface IIndexerAccessor<out TKind> : IAccessor<TKind>
    where TKind : IKnownOrNot
{
    IIndexer<TKind, IAccess> DeclaringIndexer { get; }
    AccessModifier AccessModifier { get; }

    /// <summary>
    /// Gets a list of parameters of the indexer accessor in declaration order.
    /// The resulting sequence is not guaranteed to be identical to the result
    /// of <see cref="IIndexer{TKind, TAccess}.GetParameters"/>, because it includes
    /// accessor-specific parameters (e.g. implicit 'value' parameter of setters).
    /// </summary>
    IReadOnlyList<IParameter> GetParameters();
}

/// <summary>
/// Represents an indexer accessor that acts as an assignment target.
/// </summary>
/// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IIndexerSetAccessor{TKind}"/> or <see cref="IIndexerInitAccessor{TKind}"/> instead.
/// </remarks>
public interface IIndexerWriteAccessor<out TKind> : IIndexerAccessor<TKind>
    where TKind : IKnownOrNot;

public interface IIndexerGetAccessor<out TKind> : IIndexerAccessor<TKind>
    where TKind : IKnownOrNot;

public interface IIndexerSetAccessor<out TKind> : IIndexerWriteAccessor<TKind>
    where TKind : IKnownOrNot;

public interface IIndexerInitAccessor<out TKind> : IIndexerWriteAccessor<TKind>
    where TKind : IKnownOrNot;
