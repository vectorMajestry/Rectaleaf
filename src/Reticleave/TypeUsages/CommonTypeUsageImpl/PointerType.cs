using Reticleave.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;


namespace Reticleave.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class PointerType<TKind>(IComposableType<TKind> elementType) : TypeUsage<TKind>, IPointerType<TKind>
    where TKind : IKnownOrNot
{
    public override string LocalName => WrapperNameFormatter.PointerName(ElementType.LocalName);
    public IComposableType<TKind> ElementType { get; } = elementType;
}

