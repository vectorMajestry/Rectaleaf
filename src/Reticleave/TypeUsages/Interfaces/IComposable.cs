namespace Reticleave.Metadata.TypeUsages.Interfaces;

public interface IComposableType<out TKind> : ITypeUsage<TKind>
    where TKind : IKnownOrNot;
