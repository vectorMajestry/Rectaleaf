namespace Rectaleaf.Metadata.DeclaredTypes.Comparers;

public sealed class DeclaredTypeEqualityComparer : IEqualityComparer<IDeclaredType>
{
    public static DeclaredTypeEqualityComparer Default { get; } = new();

    public bool Equals(IDeclaredType? x, IDeclaredType? y) => (x, y) switch
    {
        _ when ReferenceEquals(x, y) => true,
        (null, _) or (_, null) => false,
        (IHasGenericParameters gx, IHasGenericParameters gy)
            => x.FullyQualifiedName == y.FullyQualifiedName
            && gx.GetGenericParameters().Count == gy.GetGenericParameters().Count,
        (not IHasGenericParameters, not IHasGenericParameters) => x.FullyQualifiedName == y.FullyQualifiedName,
        _ => false,
    };

    public int GetHashCode(IDeclaredType obj) => obj switch
    {
        null => 0,
        IHasGenericParameters g => HashCode.Combine(obj.FullyQualifiedName, g.GetGenericParameters().Count),
        _ => obj.FullyQualifiedName.GetHashCode()
    };
}
