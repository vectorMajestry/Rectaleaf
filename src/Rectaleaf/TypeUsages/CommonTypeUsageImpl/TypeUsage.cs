using Rectaleaf.Metadata.TypeUsages.Comparers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public abstract class TypeUsage<TKind> : ITypeUsage<TKind>
    where TKind : IKnownOrNot
{
    public abstract string LocalName { get; }

    public override sealed bool Equals(object? obj)
        => obj is ITypeUsage<IKnownOrNot> other && TypeUsageEqualityComparer.Default.Equals((ITypeUsage<IKnownOrNot>)this, other);

    public override sealed int GetHashCode()
        => TypeUsageEqualityComparer.Default.GetHashCode((ITypeUsage<IKnownOrNot>)this);
}

