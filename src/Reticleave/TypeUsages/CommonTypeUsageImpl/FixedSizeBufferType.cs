using Geyser.Generation.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;

namespace Reticleave.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class FixedSizeBufferType(FixedSizeBufferTypeEnum kind) : TypeUsage<IKnown>, IFixedSizeBufferType
{
    public override string LocalName => field ??= Util.PredefinedTypeToString((PredefinedTypeKind)Kind);
    public FixedSizeBufferTypeEnum Kind { get; } = kind;
}

