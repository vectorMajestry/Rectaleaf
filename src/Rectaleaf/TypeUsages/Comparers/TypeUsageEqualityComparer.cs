using Rectaleaf.Components;
using Rectaleaf.Helpers;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.DeclaredTypes.Comparers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.Comparers;

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
        PointerDiscriminator = 2, ArrayDiscriminator = 3, PlaceholderDiscriminator = 4, BoundGenericDiscriminator = 5,
        RichTupleDiscriminator = 6, ChainOrObliviousTupleDiscriminator = 7,
        ManagedFuncPtrDiscriminator = 8, UnmanagedFuncPtrDiscriminator = 9;

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
        IRichTupleType<IIncomplete> rich => GetHashCodeIncompleteRichTuple(rich),
        ITupleType<IIncomplete> chain => GetHashCodeIncompleteChainTuple(chain),
        IManagedFunctionPointerType<IIncomplete> man => GetHashCodeIncompleteManagedFuncPtr(man),
        IUnmanagedFunctionPointerType<IIncomplete> unman => GetHashCodeIncompleteUnmanagedFuncPtr(unman),
        _ => throw new InvalidOperationException("Unreachable case.")
    };

    internal int GetHashCodeIncompleteBoundGeneric(IBoundGenericType<IIncomplete, IHasGenericParameters> bg)
    {
        var hash = new HashCode();
        hash.Add(BoundGenericDiscriminator);
        hash.Add(DeclaredTypeEqualityComparer.Default.GetHashCode(bg.TypeDefinition));
        if (!ignoreNullability)
            hash.Add(bg.Nullability);

        foreach (var arg in bg.TypeArguments)
            hash.Add(GetHashCode(arg));

        return hash.ToHashCode();
    }

    internal int GetHashCodeIncompleteRichTuple(IRichTupleType<IIncomplete> rich)
    {
        var hash = new HashCode();
        hash.Add(RichTupleDiscriminator);
        if (!ignoreNullability)
            hash.Add(rich.Nullability);

        foreach (var elem in rich.Elements)
        {
            hash.Add(elem.Name);
            hash.Add(GetHashCode(elem.Type));
        }

        return hash.ToHashCode();
    }

    internal int GetHashCodeIncompleteChainTuple(ITupleType<IIncomplete> chain)
    {
        var hash = new HashCode();
        hash.Add(ChainOrObliviousTupleDiscriminator);
        if (!ignoreNullability)
            hash.Add(chain.Nullability);

        foreach (var elem in chain.GetElementTypes())
            hash.Add(GetHashCode(elem));

        return hash.ToHashCode();
    }

    internal int GetHashCodeIncompleteManagedFuncPtr(IManagedFunctionPointerType<IIncomplete> man)
    {
        var hash = new HashCode();
        hash.Add(ManagedFuncPtrDiscriminator);
        hash.Add(GetHashCode(man.ReturnType));

        foreach (var param in man.Parameters)
            hash.Add(ParameterEqualityComparer.Default.GetHashCode(param));

        return hash.ToHashCode();
    }

    internal int GetHashCodeIncompleteUnmanagedFuncPtr(IUnmanagedFunctionPointerType<IIncomplete> unman)
    {
        var hash = new HashCode();
        hash.Add(UnmanagedFuncPtrDiscriminator);
        hash.Add(GetHashCode(unman.ReturnType));

        foreach (var cc in unman.CallingConventions)
            hash.Add((int)cc);

        foreach (var param in unman.Parameters)
            hash.Add(ParameterEqualityComparer.Default.GetHashCode(param));

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
            (IPlainType<IDeclaredType> xDecl, IPlainType<IDeclaredType> yDecl) => DeclaredTypeEqualityComparer.Default.Equals(xDecl.TypeDefinition, yDecl.TypeDefinition),
            (IBoundGenericType<IKnown, IHasGenericParameters> xBg, IBoundGenericType<IKnown, IHasGenericParameters> yBg)
                => DeclaredTypeEqualityComparer.Default.Equals(xBg.TypeDefinition, yBg.TypeDefinition)
                && xBg.TypeArguments.ZipAndCompare(yBg.TypeArguments, Equals),
            (IRichTupleType<IKnown> xRich, IRichTupleType<IKnown> yRich)
                => xRich.Elements.ZipAndCompare(yRich.Elements, TupleElementEquals),
            (ITupleType<IKnown> xChain, ITupleType<IKnown> yChain) when xChain is not IRichTupleType<IKnown> && yChain is not IRichTupleType<IKnown>
                => xChain.GetElementTypes().ZipAndCompare(yChain.GetElementTypes(), Equals),
            (IFunctionPointerType<IKnown> xFp, IFunctionPointerType<IKnown> yFp)
                => FunctionPointerEquals(xFp, yFp),
            _ => false
        };

    internal bool EqualsIncompleteInner(IInnerType<IIncomplete> x, IInnerType<IIncomplete> y)
        => NullabilityEquals(x, y) && (x, y) switch
        {
            (IPlaceholderType xPh, IPlaceholderType yPh) => xPh.Info.Position == yPh.Info.Position && xPh.Info.Source == yPh.Info.Source,
            (IBoundGenericType<IIncomplete, IHasGenericParameters> xBg, IBoundGenericType<IIncomplete, IHasGenericParameters> yBg)
                => DeclaredTypeEqualityComparer.Default.Equals(xBg.TypeDefinition, yBg.TypeDefinition)
                && xBg.TypeArguments.ZipAndCompare(yBg.TypeArguments, Equals),
            (IRichTupleType<IIncomplete> xRich, IRichTupleType<IIncomplete> yRich)
                => xRich.Elements.ZipAndCompare(yRich.Elements, TupleElementEquals),
            (ITupleType<IIncomplete> xChain, ITupleType<IIncomplete> yChain) when xChain is not IRichTupleType<IIncomplete> && yChain is not IRichTupleType<IIncomplete>
                => xChain.GetElementTypes().ZipAndCompare(yChain.GetElementTypes(), Equals),
            (IFunctionPointerType<IIncomplete> xFp, IFunctionPointerType<IIncomplete> yFp)
                => FunctionPointerEquals(xFp, yFp),
            _ => false
        };

    internal bool FunctionPointerEquals<TKind>(IFunctionPointerType<TKind> x, IFunctionPointerType<TKind> y)
        where TKind : IKnownOrNot
        => (x, y) switch
        {
            (IManagedFunctionPointerType<TKind> xMan, IManagedFunctionPointerType<TKind> yMan)
                => Equals(xMan.ReturnType, yMan.ReturnType)
                && xMan.Parameters.ZipAndCompare(yMan.Parameters, ParameterEqualityComparer.Default.Equals),

            (IUnmanagedFunctionPointerType<TKind> xUnman, IUnmanagedFunctionPointerType<TKind> yUnman)
                => Equals(xUnman.ReturnType, yUnman.ReturnType)
                && xUnman.CallingConventionFlags == yUnman.CallingConventionFlags
                && xUnman.CallingConventions.ZipAndCompare(yUnman.CallingConventions, (a, b) => a == b)
                && xUnman.Parameters.ZipAndCompare(yUnman.Parameters, ParameterEqualityComparer.Default.Equals),

            _ => false
        };

    bool TupleElementEquals<TKind>(ITupleElement<TKind> x, ITupleElement<TKind> y)
        where TKind : IKnownOrNot
        => x.Name == y.Name && Equals(x.Type, y.Type);

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