using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;

namespace Reticleave.Metadata.Members;

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
