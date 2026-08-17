using Geyser.Generation.Helpers;
using Reticleave.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;


namespace Reticleave.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class PredefinedType(PredefinedTypeKind kind, NullabilityKind nullability) : TypeUsage<IKnown>, IPredefinedType
{
    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
        ? WrapperNameFormatter.NullableName(Util.PredefinedTypeToString(Kind))
        : Util.PredefinedTypeToString(Kind);
    public PredefinedTypeKind Kind { get; } = kind;
    public NullabilityKind Nullability { get; } = nullability;
}

