namespace Rectaleaf.Metadata.TypeUsages.Interfaces;

public interface IComposableType<out TKind> : ITypeUsage<TKind>
    where TKind : IKnownOrNot;
