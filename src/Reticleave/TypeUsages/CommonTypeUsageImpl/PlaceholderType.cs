using Reticleave.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;


namespace Reticleave.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class PlaceholderType(IGenericParameter info, NullabilityKind nullability) : TypeUsage<IIncomplete>, IPlaceholderType
{
    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(Info.Name)
            : Info.Name;
    public IGenericParameter Info { get; } = info;
    public NullabilityKind Nullability { get; } = nullability;
}

