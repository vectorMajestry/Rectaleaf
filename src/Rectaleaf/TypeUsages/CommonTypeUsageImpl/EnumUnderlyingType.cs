using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class EnumUnderlyingType(EnumUnderlyingTypeEnum kind) : TypeUsage<IKnown>, IEnumUnderlyingType
{
    public override string LocalName => field ??= Util.PredefinedTypeToString((PredefinedTypeKind)Kind);
    public EnumUnderlyingTypeEnum Kind { get; } = kind;
}

