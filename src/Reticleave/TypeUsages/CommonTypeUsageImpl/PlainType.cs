using Reticleave.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;

namespace Reticleave.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class PlainType<TDecl>(TDecl declaredType, NullabilityKind nullability)
    : TypeUsage<IKnown>, IPlainType<TDecl>
    where TDecl : IDeclaredType
{
    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(TypeDefinition.LocalName)
            : TypeDefinition.LocalName;
    public TDecl TypeDefinition { get; } = declaredType;
    public NullabilityKind Nullability { get; } = nullability;

    IDeclaredType IDeclTypeUsage<IKnown>.TypeDefinition => TypeDefinition;
}

