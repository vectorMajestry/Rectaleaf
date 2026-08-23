using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class TupleElement<TKind>(IComposableType<TKind> type, string? name = null) : ITupleElement<TKind>
    where TKind : IKnownOrNot
{
    public IComposableType<TKind> Type { get; } = type;
    public string? Name { get; } = name;
}
