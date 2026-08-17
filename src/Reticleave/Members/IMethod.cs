using Reticleave.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;

namespace Reticleave.Metadata.Members;

public sealed class MethodEqualityComparer : IEqualityComparer<IMethod>
{
    public static MethodEqualityComparer Default { get; } = new();

    public bool Equals(IMethod? x, IMethod? y) => (x, y) switch
    {
        _ when ReferenceEquals(x, y) => true,
        (null, _) or (_, null) => false,
        _ => x.Name == y.Name
            && x.DeclaringType.Equals(y.DeclaringType)
            && x.IsGeneric == y.IsGeneric
            && (x.IsGeneric ? x.GetGenericParameters().Count == y.GetGenericParameters().Count : true)
            && x.HasArglistParameter == y.HasArglistParameter
            && x.GetParameters().ZipAndCompare(y.GetParameters(), ParameterEquals)
    };

    bool ParameterEquals(IParameter x, IParameter y)
        => x.Modifiers == y.Modifiers
        && (x.Type, y.Type) switch
        {
            (ITypeUsage<IKnown> tx, ITypeUsage<IKnown> ty) => tx.Equals(ty),
            (ITypeUsage<IIncomplete> tx, ITypeUsage<IIncomplete> ty) => IncompleteEquals(tx, ty),
            _ => false
        };

    bool IncompleteEquals(ITypeUsage<IIncomplete> x, ITypeUsage<IIncomplete> y) => (x, y) switch
    {
        (IWrapperType<IIncomplete> wx, IWrapperType<IIncomplete> wy) => IncompleteEquals(wx.ElementType, wy.ElementType),
        (IPlaceholderType px, IPlaceholderType py) => PlaceholderEquals(px, py),
        (IBoundGenericType<IIncomplete, IHasGenericParameters> gx, IBoundGenericType<IIncomplete, IHasGenericParameters> gy) => IncompleteBoundGenericEquals(gx, gy),
        _ => false
    };

    /*
        BREAKING CYCLIC DEPENDENCY:

    When comparing parameters whose types contain placeholders, we MUST NOT compare
    those types directly. Comparing placeholders that came from a method signature
    requires recursing back into the method comparison, which would cause an infinite loop:

    When comparing two methods M<T>(T a) and M<T>(T a), comparing the types of parameters 'a'
    will require comparing the positions of placeholders T and their sources, which are 
    the methods themselves. This will require comparing the methods again, and so on.

    At this point, we already know both methods share the same declaring type, name, 
    and generic arity, as well as the number of parameters, and that pairs of parameters whose
    types do not contain placeholders are equal. Therefore, for the methods to not be equal,
    the types of corresponding parameters must refer to different generic parameters. 

    IMethod strictly represents methods and not local functions, so any generic parameters 
    must come either from the signature of the method being inspected or its containing 
    type (as there is no containing method).

    If the placeholders for the types of corresponding parameters being compared come from
    the signatures of the methods being compared, then in order for them to differ, they must
    refer to generic parameters at different positions. Consider the following example:

    void M<T, U>(T a, U b) { }
    void M<U, T>(T a, U b) { }

    Placeholders for the types of corresponding parameters have the same names, but they refer to 
    generic parameters at different positions. In this case, 'T' of the first method refers to the 
    generic parameter at position 0, while 'T' of the second method refers to the generic parameter
    at position 1. Therefore, the two placeholders for the types of parameters—and, by extension, 
    the two methods—are not equal. This is handled by the first arm of the switch expression which
    only compares positions of the two generic parameters, avoiding infinite recursion.

    If the placeholders for the types of corresponding parameters being compared come from
    the signatures of the containing types of the methods being compared, then in order for them
    to differ, their declaring types must be different or their names must be different (handled 
    by the second arm of the switch expression).
    */
    bool PlaceholderEquals(IPlaceholderType x, IPlaceholderType y) => (x.Info, y.Info) switch
    {
        ({ Source: GenericMethod }, { Source: GenericMethod })
            => x.Info.Position == y.Info.Position,

        ({ Source: GenericType sx }, { Source: GenericType sy })
            => x.Info.Position == y.Info.Position && sx == sy,

        // Methods are never nested in local functions, so generic parameters cannot come from a local function.

        _ => false
    };

    bool IncompleteBoundGenericEquals(IBoundGenericType<IIncomplete, IHasGenericParameters> x, IBoundGenericType<IIncomplete, IHasGenericParameters> y)
        => x.TypeDefinition.Equals(y.TypeDefinition)
        && x.TypeArguments.ZipAndCompare(y.TypeArguments, (tx, ty) => (tx, ty) switch
        {
            (IComposableType<IKnown> knownTypeArgX, IComposableType<IKnown> knownTypeArgY) => knownTypeArgX.Equals(knownTypeArgY),
            (IComposableType<IIncomplete> incompleteTypeArgX, IComposableType<IIncomplete> incompleteTypeArgY)
                => IncompleteEquals(incompleteTypeArgX, incompleteTypeArgY),
            _ => false
        });

    public int GetHashCode(IMethod obj) => obj.IsGeneric
            ? HashCode.Combine(obj.Name, obj.DeclaringType, obj.GetParameters().Count, obj.GetGenericParameters().Count)
            : HashCode.Combine(obj.Name, obj.DeclaringType, obj.GetParameters().Count);
}

public interface IMethod : IHasLocalFunctions
{
    string Name { get; }
    IReturnInfo<IKnownOrNot> ReturnInfo { get; }
    IHasMethods DeclaringType { get; }
    bool IsGeneric { get; }
    bool IsReadOnly { get; }
    bool HasArglistParameter { get; }
    AccessModifier AccessModifier { get; }
    IInheritanceKind<IMethod> Inheritance { get; }

    IReadOnlyList<IParameter> GetParameters();
    IReadOnlyList<IGenericParameter> GetGenericParameters();
    IReadOnlyList<IAttribute> GetAttributes();
}

public interface IStaticMethod : IMethod;

public interface IInstMethod : IMethod;
