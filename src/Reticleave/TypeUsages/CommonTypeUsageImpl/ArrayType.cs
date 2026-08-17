using Reticleave.Metadata.TypeUsages.Interfaces;


namespace Reticleave.Metadata.TypeUsages.CommonTypeUsageImpl;

public abstract class ArrayType<TKind, TRankKind>(IComposableType<TKind> elementType, NullabilityKind nullability) : TypeUsage<TKind>, IArrayType<TKind, TRankKind>
    where TKind : IKnownOrNot
    where TRankKind : IFlatOrNot
{
    public IComposableType<TKind> ElementType { get; } = elementType;
    public NullabilityKind Nullability { get; } = nullability;
    public abstract int Rank { get; }
}

