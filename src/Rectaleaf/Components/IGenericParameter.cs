using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Helpers;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Comparers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Components;

public interface IGenericParameter
{
    string Name { get; }

    int Position { get; }

    bool HasReferenceTypeConstraint { get; }
    bool HasNullableReferenceTypeConstraint { get; }
    bool HasNotNullConstraint { get; }
    bool HasValueTypeConstraint { get; }
    bool HasUnmanagedTypeConstraint { get; }
    bool HasConstructorConstraint { get; }
    bool HasAllowsRefStructConstraint { get; }
    bool HasDefaultConstraint { get; }

    GenericVariance Variance { get; }

    IReadOnlyList<IValidConstraint> ConstraintTypes { get; }

    GenericParameterSource Source { get; }
}

public enum GenericVariance
{
    Invariant,
    Covariant,
    Contravariant
}

public sealed class GenericParameterEqualityComparer : IEqualityComparer<IGenericParameter>
{
    /// <summary>
    /// Compares generic parameters by name and source. If the two parameters have a common source and the same name, 
    /// they are guaranteed to have the same constraints and variance, so they are not compared.
    /// </summary>
    public static GenericParameterEqualityComparer Default { get; } = new(includeSource: true);

    /// <summary>
    /// Compares generic parameters by name, variance, and constraints, ignoring the source of the generic parameter.
    /// </summary>
    public static GenericParameterEqualityComparer IgnoreSource { get; } = new(includeSource: false);

    readonly bool includeSource;

    GenericParameterEqualityComparer(bool includeSource)
    {
        this.includeSource = includeSource;
    }

    public bool Equals(IGenericParameter? x, IGenericParameter? y) => (x, y) switch
    {
        _ when ReferenceEquals(x, y) => true,
        (null, _) or (_, null) => false,
        _ => x.Name == y.Name && (includeSource ? x.Source == y.Source : (x.Variance == y.Variance && AreConstraintsEqual(x, y)))
    };

    static bool AreConstraintsEqual(IGenericParameter x, IGenericParameter y)
    {
        if (GetConstraintFlags(x) != GetConstraintFlags(y))
            return false;

        if (x.ConstraintTypes.Count != y.ConstraintTypes.Count)
            return false;

        return x.ConstraintTypes.ZipAndCompare(y.ConstraintTypes, ConstraintEquals);
    }

    static bool ConstraintEquals(IValidConstraint x, IValidConstraint y) => (x, y) switch
    {
        _ when ReferenceEquals(x, y) => true,
        (null, _) or (_, null) => false,
        (ITypeUsage<IKnownOrNot> tx, ITypeUsage<IKnownOrNot> ty) => TypeUsageEqualityComparer.Default.Equals(tx, ty),
        _ => x.Equals(y)
    };

    static int GetConstraintFlags(IGenericParameter p)
    {
        return (p.HasReferenceTypeConstraint ? 1 << 0 : 0)
            | (p.HasNullableReferenceTypeConstraint ? 1 << 1 : 0)
            | (p.HasNotNullConstraint ? 1 << 2 : 0)
            | (p.HasValueTypeConstraint ? 1 << 3 : 0)
            | (p.HasUnmanagedTypeConstraint ? 1 << 4 : 0)
            | (p.HasConstructorConstraint ? 1 << 5 : 0)
            | (p.HasAllowsRefStructConstraint ? 1 << 6 : 0)
            | (p.HasDefaultConstraint ? 1 << 7 : 0);
    }

    public int GetHashCode(IGenericParameter obj)
    {
        if (obj is null)
            return 0;

        if (includeSource)
            return HashCode.Combine(obj.Name, obj.Source);

        return HashCode.Combine(obj.Name, GetConstraintFlags(obj), obj.Variance, obj.ConstraintTypes.Count);
    }
}

public abstract record GenericParameterSource;
public record GenericType(IDeclaredType Type) : GenericParameterSource;
public record GenericMethod(IMethod Method) : GenericParameterSource;
public record GenericLocalFunction(ILocalFunction LocalFunction) : GenericParameterSource;

