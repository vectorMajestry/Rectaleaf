using Geyser.Generation.Helpers;
using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;


namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class PredefinedType(PredefinedTypeKind kind, NullabilityKind nullability) : TypeUsage<IKnown>, IPredefinedType
{
    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
        ? WrapperNameFormatter.NullableName(Util.PredefinedTypeToString(Kind))
        : Util.PredefinedTypeToString(Kind);
    public PredefinedTypeKind Kind { get; } = kind;
    public NullabilityKind Nullability { get; } = nullability;
}

