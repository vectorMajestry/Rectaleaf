using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Components;

public interface IReturnInfo<out TKind>
    where TKind : IKnownOrNot
{
    ITypeUsage<TKind> Type { get; }
    IReadOnlyList<IAttribute> GetAttributes();
}
