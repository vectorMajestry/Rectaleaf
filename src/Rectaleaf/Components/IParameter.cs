using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Components;

public interface IParameter
{
    /// <summary>
    /// Gets the type usage of this parameter.
    /// When the parameter has a by-ref modifier (<c>ref</c>, <c>in</c>, <c>out</c>, or <c>ref readonly</c>),
    /// this property is wrapped in <see cref="IByRefType{TKind, TMutability}"/>.
    /// </summary>
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
