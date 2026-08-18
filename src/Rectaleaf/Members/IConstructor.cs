using Rectaleaf.Components;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Metadata.DeclaredTypes;

namespace Rectaleaf.Metadata.Members;

public interface IConstructor : IHasLocalFunctions
{
    IDeclaredType DeclaringType { get; }
    IReadOnlyList<IAttribute> GetAttributes();
}

public interface IInstanceConstructor : IConstructor
{
    new IHasInstanceConstructors DeclaringType { get; }
    AccessModifier AccessModifier { get; }
    bool IsPrimary { get; }
    bool HasArglistParameter { get; }
    IReadOnlyList<IParameter> GetParameters();
}

public interface IStaticConstructor : IConstructor
{
    new IHasStaticConstructor DeclaringType { get; }
}
