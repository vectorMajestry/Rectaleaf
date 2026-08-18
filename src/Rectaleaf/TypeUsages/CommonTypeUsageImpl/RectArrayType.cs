using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class RectArrayType<TKind> : ArrayType<TKind, IRectArr>
    where TKind : IKnownOrNot
{
    public override int Rank { get; }

    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(WrapperNameFormatter.ArrayName(ElementType.LocalName, Rank))
            : WrapperNameFormatter.ArrayName(ElementType.LocalName, Rank);

    public RectArrayType(IComposableType<TKind> elementType, int rank, NullabilityKind nullability) : base(elementType, nullability)
    {
        if (rank < 2)
            throw new ArgumentOutOfRangeException("Rank must be at least 2.");
        Rank = rank;
    }
}

