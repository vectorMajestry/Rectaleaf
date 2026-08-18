namespace Rectaleaf.Metadata.TypeUsages.Interfaces;

public interface IByRefType<out TKind, out TMutability> : ITypeUsage<TKind>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    public IComposableType<TKind> ElementType { get; }
}
