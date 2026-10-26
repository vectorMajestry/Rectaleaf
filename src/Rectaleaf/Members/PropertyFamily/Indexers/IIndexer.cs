using Rectaleaf.Components;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.Members.PropertyFamily.Indexers;

public interface IIndexer<out TKind, out TAccess>
    where TKind : IKnownOrNot
    where TAccess : IAccess
{
    string Name { get; }
    IHasIndexers DeclaringType { get; }
    AccessModifier AccessModifier { get; }
    IInheritanceKind<IIndexer<TKind, TAccess>> Inheritance { get; }
    IReadOnlyList<IAttribute> GetAttributes();
    IReadOnlyList<IParameter> GetParameters();

    /// <summary>
    /// An alternative way of querying for indexer accessors. Accessors queried this way are returned as
    /// <see cref="IAccessor{TKind}"/> where TKind is <see cref="IKnownOrNot"/>. Use extension properties
    /// from <see cref="IndexerExtensions"/> instead.
    /// </summary>
    [Obsolete]
    TAccess Accessors { get; }
}

public interface IRefIndexer<out TKind, out TMutability> : IIndexer<TKind, IGet>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    IByRefType<TKind, TMutability> Type { get; }
}

public interface IRegularIndexer<out TKind, out TAccess> : IIndexer<TKind, TAccess>
    where TKind : IKnownOrNot
    where TAccess : IAccess
{
    IComposableType<TKind> Type { get; }
}

public static class IndexerExtensions
{
#pragma warning disable CS0612

    extension<TKind>(IRefIndexer<TKind, IMutOrNot> refIndexer)
        where TKind : IKnownOrNot
    {
        public IIndexerGetAccessor<TKind> Getter => (IIndexerGetAccessor<TKind>)refIndexer.Accessors.Getter;
    }

    extension<TKind>(IRegularIndexer<TKind, IGet> indexer)
        where TKind : IKnownOrNot
    {
        public IIndexerGetAccessor<TKind> Getter => (IIndexerGetAccessor<TKind>)indexer.Accessors.Getter;
    }

    extension<TKind>(IRegularIndexer<TKind, ISet> indexer)
        where TKind : IKnownOrNot
    {
        public IIndexerSetAccessor<TKind> Setter => (IIndexerSetAccessor<TKind>)indexer.Accessors.Setter;
    }

    extension<TKind>(IRegularIndexer<TKind, IInit> indexer)
        where TKind : IKnownOrNot
    {
        public IIndexerInitAccessor<TKind> InitAccessor => (IIndexerInitAccessor<TKind>)indexer.Accessors.InitAccessor;
    }
#pragma warning restore CS0612
}
