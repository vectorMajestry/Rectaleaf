using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Components;

namespace Rectaleaf.Metadata.Members;

/// <summary>
/// Represents a local function (a function defined inside another method, constructor, etc.).
/// </summary>
public interface ILocalFunction : IHasLocalFunctions
{
    string Name { get; }

    /// <summary>
    /// Gets the zero-based declaration ordinal of this local function within its declaring entity.
    /// </summary>
    int Ordinal { get; }

    IReturnInfo<IKnownOrNot> ReturnInfo { get; }
    IHasLocalFunctions DeclaringEntity { get; }
    bool IsStatic { get; }
    bool IsGeneric { get; }

    IReadOnlyList<IParameter> GetParameters();
    IReadOnlyList<IGenericParameter> GetGenericParameters();
    IReadOnlyList<IAttribute> GetAttributes();
}

public sealed class LocalFunctionEqualityComparer : IEqualityComparer<ILocalFunction>
{
    public static LocalFunctionEqualityComparer Default { get; } = new();

    public bool Equals(ILocalFunction? x, ILocalFunction? y) => (x, y) switch
    {
        _ when ReferenceEquals(x, y) => true,
        (null, _) or (_, null) => false,
        _ => x.Ordinal == y.Ordinal && x.DeclaringEntity.Equals(y.DeclaringEntity)
    };

    public int GetHashCode(ILocalFunction obj)
        => HashCode.Combine(obj.Ordinal, obj.DeclaringEntity);
}

