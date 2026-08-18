using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;


namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class LinearArrayType<TKind>(IComposableType<TKind> elementType, NullabilityKind nullability) : ArrayType<TKind, IFlat>(elementType, nullability)
    where TKind : IKnownOrNot
{
    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(WrapperNameFormatter.ArrayName(ElementType.LocalName))
            : WrapperNameFormatter.ArrayName(ElementType.LocalName);
    public override int Rank => 1;
}

