using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Components;


namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class PlaceholderType(IGenericParameter info, NullabilityKind nullability) : TypeUsage<IIncomplete>, IPlaceholderType
{
    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(Info.Name)
            : Info.Name;
    public IGenericParameter Info { get; } = info;
    public NullabilityKind Nullability { get; } = nullability;
}

