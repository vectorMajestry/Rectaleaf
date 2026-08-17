using Reticleave.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;


namespace Reticleave.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class ByRefType<TKind, TMutability>(IComposableType<TKind> elementType)
    : TypeUsage<TKind>, IByRefType<TKind, TMutability>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    static readonly bool IsReadOnlyType = typeof(TMutability) == typeof(IReadOnly);
    public override string LocalName => IsReadOnlyType ? WrapperNameFormatter.RefReadOnlyName(ElementType.LocalName) : WrapperNameFormatter.RefName(ElementType.LocalName);
    public IComposableType<TKind> ElementType { get; } = elementType;
}

