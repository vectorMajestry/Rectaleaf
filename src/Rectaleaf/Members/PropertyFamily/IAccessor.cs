using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Components;

namespace Rectaleaf.Metadata.Members.PropertyFamily;

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
