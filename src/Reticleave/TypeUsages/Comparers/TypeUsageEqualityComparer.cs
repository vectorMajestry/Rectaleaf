using Reticleave.Helpers;
using Reticleave.Metadata.TypeUsages.Interfaces;

namespace Reticleave.Metadata.TypeUsages.Comparers;

public sealed class TypeUsageEqualityComparer : IEqualityComparer<ITypeUsage<IKnownOrNot>>
{
    public static TypeUsageEqualityComparer Default { get; } = new(false);

    public static TypeUsageEqualityComparer IgnoreNullability { get; } = new(true);

    TypeUsageEqualityComparer(bool ignoreNullability)
    {
        this.ignoreNullability = ignoreNullability;
    }

    readonly bool ignoreNullability;

    public bool Equals(ITypeUsage<IKnownOrNot>? x, ITypeUsage<IKnownOrNot>? y) => (x, y) switch
    {
        _ when ReferenceEquals(x, y) => true,
        (null, _) or (_, null) => false,
        (ITypeUsage<IKnown> xKnown, ITypeUsage<IKnown> yKnown) => EqualsKnown(xKnown, yKnown),
        (ITypeUsage<IIncomplete> xIncomplete, ITypeUsage<IIncomplete> yIncomplete) => EqualsIncomplete(xIncomplete, yIncomplete),
        _ => false
    };

    public int GetHashCode(ITypeUsage<IKnownOrNot> obj)
        => obj is ITypeUsage<IKnown> known
            ? known.FullyQualifiedName.GetHashCode()
            : GetHashCodeIncomplete((ITypeUsage<IIncomplete>)obj);

    const int ByRefMutDiscriminator = 0, ByRefReadOnlyDiscriminator = 1,
        PointerDiscriminator = 2, ArrayDiscriminator = 3, PlaceholderDiscriminator = 4, BoundGenericDiscriminator = 5;

    internal int GetHashCodeIncomplete(ITypeUsage<IIncomplete> x) => x switch
    {
        IByRefType<IIncomplete, IMutOrNot> byRef => GetHashCodeIncompleteByRef(byRef),
        IComposableType<IIncomplete> composable => GetHashCodeIncompleteComposable(composable),
        _ => throw new InvalidOperationException("Unreachable case.")
    };

    internal int GetHashCodeIncompleteByRef(IByRefType<IIncomplete, IMutOrNot> x) => x switch
    {
        IByRefType<IIncomplete, IMut> mut => HashCode.Combine(
            ByRefMutDiscriminator, GetHashCodeIncompleteComposable(mut.ElementType)),
        IByRefType<IIncomplete, IReadOnly> ro => HashCode.Combine(
            ByRefReadOnlyDiscriminator, GetHashCodeIncompleteComposable(ro.ElementType)),
        _ => throw new InvalidOperationException("Unreachable case.")
    };

    internal int GetHashCodeIncompleteComposable(IComposableType<IIncomplete> x) => x switch
    {
        IWrapperType<IIncomplete> wrapper => GetHashCodeIncompleteWrapper(wrapper),
        IInnerType<IIncomplete> inner => GetHashCodeIncompleteInner(inner),
        _ => throw new InvalidOperationException("Unreachable case.")
    };

    internal int GetHashCodeIncompleteWrapper(IWrapperType<IIncomplete> x) => x switch
    {
        IPointerType<IIncomplete> p => HashCode.Combine(
            PointerDiscriminator, GetHashCodeIncompleteComposable(p.ElementType)),
        IArrayType<IIncomplete, IFlatOrNot> arr =>
            ignoreNullability
                ? HashCode.Combine(ArrayDiscriminator, arr.Rank, GetHashCodeIncompleteComposable(arr.ElementType))
                : HashCode.Combine(ArrayDiscriminator, arr.Rank, (int)arr.Nullability, GetHashCodeIncompleteComposable(arr.ElementType)),
        _ => throw new InvalidOperationException("Unreachable case.")
    };

    internal int GetHashCodeIncompleteInner(IInnerType<IIncomplete> x) => x switch
    {
        IPlaceholderType p =>
            ignoreNullability
                ? HashCode.Combine(PlaceholderDiscriminator, p.Info.Position, p.Info.Source)
                : HashCode.Combine(PlaceholderDiscriminator, (int)p.Nullability, p.Info.Position, p.Info.Source),
        IBoundGenericType<IIncomplete, IHasGenericParameters> bg => GetHashCodeIncompleteBoundGeneric(bg),
        _ => throw new InvalidOperationException("Unreachable case.")
    };

    internal int GetHashCodeIncompleteBoundGeneric(IBoundGenericType<IIncomplete, IHasGenericParameters> bg)
    {
        var hash = new HashCode();
        hash.Add(BoundGenericDiscriminator);
        hash.Add(bg.TypeDefinition.FullyQualifiedName);
        hash.Add(bg.TypeArguments.Count);
        if (!ignoreNullability)
            hash.Add(bg.Nullability);

        foreach (var arg in bg.TypeArguments)
            hash.Add(GetHashCode(arg));

        return hash.ToHashCode();
    }

    internal bool EqualsKnown(ITypeUsage<IKnown> x, ITypeUsage<IKnown> y)
        => (x, y) switch
        {
            (IByRefType<IKnown, IMutOrNot> xByRef, IByRefType<IKnown, IMutOrNot> yByRef) => EqualsKnownByRef(xByRef, yByRef),
            (IComposableType<IKnown> xComp, IComposableType<IKnown> yComp) => EqualsKnownComposable(xComp, yComp),
            _ => false
        };

    internal bool EqualsIncomplete(ITypeUsage<IIncomplete> x, ITypeUsage<IIncomplete> y)
        => (x, y) switch
        {
            (IByRefType<IIncomplete, IMutOrNot> xByRef, IByRefType<IIncomplete, IMutOrNot> yByRef) => EqualsIncompleteByRef(xByRef, yByRef),
            (IComposableType<IIncomplete> xComp, IComposableType<IIncomplete> yComp) => EqualsIncompleteComposable(xComp, yComp),
            _ => false
        };

    internal bool EqualsKnownByRef(IByRefType<IKnown, IMutOrNot> x, IByRefType<IKnown, IMutOrNot> y)
        => (x, y) switch
        {
            (IByRefType<IKnown, IMut> xMut, IByRefType<IKnown, IMut> yMut) => EqualsKnownComposable(xMut.ElementType, yMut.ElementType),
            (IByRefType<IKnown, IReadOnly> xRo, IByRefType<IKnown, IReadOnly> yRo) => EqualsKnownComposable(xRo.ElementType, yRo.ElementType),
            _ => false
        };

    internal bool EqualsIncompleteByRef(IByRefType<IIncomplete, IMutOrNot> x, IByRefType<IIncomplete, IMutOrNot> y)
        => (x, y) switch
        {
            (IByRefType<IIncomplete, IMut> xMut, IByRefType<IIncomplete, IMut> yMut) => EqualsIncompleteComposable(xMut.ElementType, yMut.ElementType),
            (IByRefType<IIncomplete, IReadOnly> xRo, IByRefType<IIncomplete, IReadOnly> yRo) => EqualsIncompleteComposable(xRo.ElementType, yRo.ElementType),
            _ => false
        };

    internal bool EqualsKnownComposable(IComposableType<IKnown> x, IComposableType<IKnown> y)
        => (x, y) switch
        {
            (IWrapperType<IKnown> xWrapper, IWrapperType<IKnown> yWrapper) => EqualsKnownWrapper(xWrapper, yWrapper),
            (IInnerType<IKnown> xInner, IInnerType<IKnown> yInner) => EqualsKnownInner(xInner, yInner),
            _ => false
        };

    internal bool EqualsIncompleteComposable(IComposableType<IIncomplete> x, IComposableType<IIncomplete> y)
        => (x, y) switch
        {
            (IWrapperType<IIncomplete> xWrapper, IWrapperType<IIncomplete> yWrapper) => EqualsIncompleteWrapper(xWrapper, yWrapper),
            (IInnerType<IIncomplete> xInner, IInnerType<IIncomplete> yInner) => EqualsIncompleteInner(xInner, yInner),
            _ => false
        };

    internal bool EqualsKnownWrapper(IWrapperType<IKnown> x, IWrapperType<IKnown> y)
        => NullabilityEquals(x, y) && (x, y) switch
        {
            (IPointerType<IKnown> xPtr, IPointerType<IKnown> yPtr) => EqualsKnownComposable(xPtr.ElementType, yPtr.ElementType),
            (IArrayType<IKnown, IFlatOrNot> xArr, IArrayType<IKnown, IFlatOrNot> yArr) => xArr.Rank == yArr.Rank && EqualsKnownComposable(xArr.ElementType, yArr.ElementType),
            _ => false
        };

    internal bool EqualsIncompleteWrapper(IWrapperType<IIncomplete> x, IWrapperType<IIncomplete> y)
        => NullabilityEquals(x, y) && (x, y) switch
        {
            (IPointerType<IIncomplete> xPtr, IPointerType<IIncomplete> yPtr) => EqualsIncompleteComposable(xPtr.ElementType, yPtr.ElementType),
            (IArrayType<IIncomplete, IFlatOrNot> xArr, IArrayType<IIncomplete, IFlatOrNot> yArr) => xArr.Rank == yArr.Rank && EqualsIncompleteComposable(xArr.ElementType, yArr.ElementType),
            _ => false
        };

    internal bool EqualsKnownInner(IInnerType<IKnown> x, IInnerType<IKnown> y)
        => NullabilityEquals(x, y) && (x, y) switch
        {
            (IPredefinedType xPre, IPredefinedType yPre) => xPre.Kind == yPre.Kind,
            (IFixedSizeBufferType xFsb, IFixedSizeBufferType yFsb) => xFsb.Kind == yFsb.Kind,
            (IEnumUnderlyingType xEnum, IEnumUnderlyingType yEnum) => xEnum.Kind == yEnum.Kind,
            (IPlainType<IDeclaredType> xDecl, IPlainType<IDeclaredType> yDecl) => xDecl.TypeDefinition.FullyQualifiedName == yDecl.TypeDefinition.FullyQualifiedName,
            (IBoundGenericType<IKnown, IHasGenericParameters> xBg, IBoundGenericType<IKnown, IHasGenericParameters> yBg)
                => xBg.TypeDefinition.FullyQualifiedName == yBg.TypeDefinition.FullyQualifiedName
                && xBg.TypeArguments.Count == yBg.TypeArguments.Count
                && xBg.TypeArguments.ZipAndCompare(yBg.TypeArguments, Equals),
            _ => false
        };

    internal bool EqualsIncompleteInner(IInnerType<IIncomplete> x, IInnerType<IIncomplete> y)
        => NullabilityEquals(x, y) && (x, y) switch
        {
            (IPlaceholderType xPh, IPlaceholderType yPh) => xPh.Info.Position == yPh.Info.Position && xPh.Info.Source == yPh.Info.Source,
            (IBoundGenericType<IIncomplete, IHasGenericParameters> xBg, IBoundGenericType<IIncomplete, IHasGenericParameters> yBg)
                => xBg.TypeDefinition.FullyQualifiedName == yBg.TypeDefinition.FullyQualifiedName
                && xBg.TypeArguments.Count == yBg.TypeArguments.Count
                && xBg.TypeArguments.ZipAndCompare(yBg.TypeArguments, Equals),
            _ => false
        };

    internal bool NullabilityEquals(ITypeUsage<IKnownOrNot> x, ITypeUsage<IKnownOrNot> y)
        => ignoreNullability || (x, y) switch
        {
            (IHasNullability xNull, IHasNullability yNull) => xNull.Nullability == yNull.Nullability,

            // Nullability cannot be a differentiating factor
            (not IHasNullability, not IHasNullability) => true,

            // One supports nullability, the other does not
            _ => false
        };
}

class Test
{
    void H<T>((int j, bool k) data) where T : struct
    {


    }

    readonly record struct R(int X, int Y);

    static void Main()
    {
        var test = new Test();
        (int l, bool o) data = (5, true);
        test.H<R>(data);
    }
}
