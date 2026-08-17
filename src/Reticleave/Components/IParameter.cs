using Reticleave.Metadata;
using Reticleave.Metadata.Members;
using Reticleave.Metadata.TypeUsages.Interfaces;

namespace Reticleave.Components;

public interface IParameter
{
    ITypeUsage<IKnownOrNot> Type { get; }
    string Name { get; }
    ParameterModifiers Modifiers { get; }

    bool TryGetDefaultValue(out object? defaultValue);

    IReadOnlyList<IAttribute> GetAttributes();
}

public sealed class ParameterEqualityComparer : IEqualityComparer<IParameter>
{
    public static ParameterEqualityComparer Default { get; } = new(ignoreName: false);

    public static ParameterEqualityComparer IgnoreName { get; } = new(ignoreName: true);

    bool ignoreName;

    ParameterEqualityComparer(bool ignoreName)
    {
        this.ignoreName = ignoreName;
    }

    public bool Equals(IParameter? x, IParameter? y) => (x, y) switch
    {
        _ when ReferenceEquals(x, y) => true,
        (null, _) or (_, null) => false,
        _ => (ignoreName || x.Name == y.Name) && x.Type.Equals(y.Type) && x.Modifiers == y.Modifiers
    };

    public int GetHashCode(IParameter obj) => ignoreName ? HashCode.Combine(obj.Type, obj.Modifiers) : HashCode.Combine(obj.Name, obj.Type, obj.Modifiers);
}
