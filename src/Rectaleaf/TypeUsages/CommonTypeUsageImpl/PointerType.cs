using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;


namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class PointerType<TKind>(IComposableType<TKind> elementType) : TypeUsage<TKind>, IPointerType<TKind>
    where TKind : IKnownOrNot
{
    public override string LocalName => WrapperNameFormatter.PointerName(ElementType.LocalName);
    public IComposableType<TKind> ElementType { get; } = elementType;
}

