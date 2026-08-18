using Geyser.Generation.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class FixedSizeBufferType(FixedSizeBufferTypeEnum kind) : TypeUsage<IKnown>, IFixedSizeBufferType
{
    public override string LocalName => field ??= Util.PredefinedTypeToString((PredefinedTypeKind)Kind);
    public FixedSizeBufferTypeEnum Kind { get; } = kind;
}

