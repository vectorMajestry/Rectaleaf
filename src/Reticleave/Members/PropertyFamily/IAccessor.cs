using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;

namespace Reticleave.Metadata.Members.PropertyFamily;

/// <summary>
/// Represents any accessor (e.g. property getter, indexer setter, event add).
/// </summary>
public interface IAccessor<out TKind> : IHasLocalFunctions
    where TKind : IKnownOrNot
{
    IReadOnlyList<IAttribute> GetAttributes();
    IReturnInfo<TKind> ReturnInfo { get; }
    bool IsReadOnly { get; }
}
