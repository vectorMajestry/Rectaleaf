using Reticleave.Metadata.Members;
using Reticleave.Metadata.TypeUsages.Interfaces;

namespace Reticleave.Components;

public interface IReturnInfo<out TKind>
    where TKind : IKnownOrNot
{
    ITypeUsage<TKind> Type { get; }
    IReadOnlyList<IAttribute> GetAttributes();
}
