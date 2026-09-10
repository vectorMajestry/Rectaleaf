using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.TypeUsages;

internal static class RoslynTypeUsageFactory
{
    public static ITypeUsage<IKnownOrNot> Create(ITypeSymbol symbol, NullableAnnotation nullableAnnotation = NullableAnnotation.None)
    {
        if (symbol is null)
            throw new ArgumentNullException(nameof(symbol));

        // Handle System.Nullable<T>
        if (symbol is INamedTypeSymbol namedNullable && namedNullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var innerType = namedNullable.TypeArguments[0];
            return Create(innerType, NullableAnnotation.Annotated);
        }

        var nullability = RoslynSymbolMapping.ToNullabilityKind(
            nullableAnnotation != NullableAnnotation.None ? nullableAnnotation : symbol.NullableAnnotation);

        if (symbol.IsValueType && symbol.TypeKind != TypeKind.TypeParameter)
            nullability = NullabilityKind.NonNullable;

        // Special / Predefined types
        if (symbol.SpecialType == SpecialType.System_Void)
            return new PredefinedType(PredefinedTypeKind.Void, NullabilityKind.NonNullable);

        var predefinedKind = RoslynSymbolMapping.ToPredefinedTypeKind(symbol.SpecialType);
        if (predefinedKind.HasValue)
            return new PredefinedType(predefinedKind.Value, nullability);

        // Generic type parameter (placeholder)
        if (symbol is ITypeParameterSymbol typeParam)
            return new PlaceholderType(new RoslynGenericParameter(typeParam), nullability);

        // Array
        if (symbol is IArrayTypeSymbol arraySymbol)
            return CreateArray(arraySymbol, nullability);

        // Pointer
        if (symbol is IPointerTypeSymbol pointerSymbol)
            return CreatePointer(pointerSymbol);

        // Function Pointer
        if (symbol is IFunctionPointerTypeSymbol fpSymbol)
            return CreateFunctionPointer(fpSymbol);

        // Named types: Tuples, Bound Generics, Plain Types
        if (symbol is INamedTypeSymbol namedType)
        {
            if (namedType.IsTupleType)
                return CreateRichTuple(namedType, nullability);

            if (!namedType.IsTupleType && namedType.Name == "ValueTuple" && namedType.ContainingNamespace?.ToDisplayString() == "System")
                return CreateExplicitValueTuple(namedType, nullability);

            if (namedType.IsGenericType && !namedType.IsUnboundGenericType && namedType.TypeArguments.Length > 0 && !SymbolEqualityComparer.Default.Equals(namedType, namedType.OriginalDefinition))
                return CreateBoundGeneric(namedType, nullability);

            var decl = RectaleafRoslyn.FromSymbol(namedType.IsUnboundGenericType ? namedType.OriginalDefinition : namedType);
            return CreatePlainType(decl, nullability);
        }

        throw new NotSupportedException($"Unsupported Roslyn type symbol: {symbol} (Kind: {symbol.TypeKind})");
    }

    static ITypeUsage<IKnownOrNot> CreateArray(IArrayTypeSymbol arraySymbol, NullabilityKind nullability)
    {
        var elemUsage = (IComposableType<IKnownOrNot>)Create(arraySymbol.ElementType, arraySymbol.ElementNullableAnnotation);

        if (arraySymbol.IsSZArray || arraySymbol.Rank == 1)
        {
            if (elemUsage is IComposableType<IKnown> knownElem)
                return new LinearArrayType<IKnown>(knownElem, nullability);

            if (elemUsage is IComposableType<IIncomplete> incElem)
                return new LinearArrayType<IIncomplete>(incElem, nullability);

            return new LinearArrayType<IKnownOrNot>(elemUsage, nullability);
        }

        if (elemUsage is IComposableType<IKnown> knownRectElem)
            return new RectArrayType<IKnown>(knownRectElem, arraySymbol.Rank, nullability);

        if (elemUsage is IComposableType<IIncomplete> incRectElem)
            return new RectArrayType<IIncomplete>(incRectElem, arraySymbol.Rank, nullability);

        return new RectArrayType<IKnownOrNot>(elemUsage, arraySymbol.Rank, nullability);
    }

    static ITypeUsage<IKnownOrNot> CreatePointer(IPointerTypeSymbol pointerSymbol)
    {
        var elemUsage = (IComposableType<IKnownOrNot>)Create(pointerSymbol.PointedAtType, pointerSymbol.PointedAtType.NullableAnnotation);

        if (elemUsage is IComposableType<IKnown> knownElem)
            return new PointerType<IKnown>(knownElem);

        if (elemUsage is IComposableType<IIncomplete> incElem)
            return new PointerType<IIncomplete>(incElem);

        return new PointerType<IKnownOrNot>(elemUsage);
    }

    static ITypeUsage<IKnownOrNot> CreateFunctionPointer(IFunctionPointerTypeSymbol fpSymbol)
    {
        var parameters = fpSymbol.Signature.Parameters.Select(p => (IParameter)new RoslynParameter(p)).ToList();
        var rawReturnType = Create(fpSymbol.Signature.ReturnType, fpSymbol.Signature.ReturnNullableAnnotation);

        ITypeUsage<IKnownOrNot> returnType = rawReturnType;
        if (rawReturnType is IComposableType<IKnownOrNot> composable)
        {
            returnType = fpSymbol.Signature.RefKind switch
            {
                RefKind.Ref => (ITypeUsage<IKnownOrNot>)CreateByRef(composable, isReadOnly: false),
                RefKind.RefReadOnly or RefKind.In => (ITypeUsage<IKnownOrNot>)CreateByRef(composable, isReadOnly: true),
                _ => rawReturnType
            };
        }

        if (fpSymbol.Signature.CallingConvention == SignatureCallingConvention.Unmanaged)
        {
            var convs = fpSymbol.Signature.UnmanagedCallingConventionTypes
                .Select(t => RoslynSymbolMapping.ToCallConvKind(t.Name))
                .Where(k => k.HasValue)
                .Select(k => k!.Value)
                .ToList();

            var flags = RoslynSymbolMapping.ToCallConvFlags(convs);

            if (returnType is ITypeUsage<IKnown> knownReturn)
                return new UnmanagedFunctionPointerType<IKnown>(knownReturn, parameters, convs, flags);

            if (returnType is ITypeUsage<IIncomplete> incReturn)
                return new UnmanagedFunctionPointerType<IIncomplete>(incReturn, parameters, convs, flags);

            return new UnmanagedFunctionPointerType<IKnownOrNot>(returnType, parameters, convs, flags);
        }

        if (returnType is ITypeUsage<IKnown> knownMngReturn)
            return new ManagedFunctionPointerType<IKnown>(knownMngReturn, parameters);

        if (returnType is ITypeUsage<IIncomplete> incMngReturn)
            return new ManagedFunctionPointerType<IIncomplete>(incMngReturn, parameters);

        return new ManagedFunctionPointerType<IKnownOrNot>(returnType, parameters);
    }

    static object CreateByRef(IComposableType<IKnownOrNot> element, bool isReadOnly)
    {
        if (isReadOnly)
        {
            if (element is IComposableType<IKnown> knownElem)
                return new ByRefType<IKnown, IReadOnly>(knownElem);

            if (element is IComposableType<IIncomplete> incElem)
                return new ByRefType<IIncomplete, IReadOnly>(incElem);

            return new ByRefType<IKnownOrNot, IReadOnly>(element);
        }
        else
        {
            if (element is IComposableType<IKnown> knownElem)
                return new ByRefType<IKnown, IMut>(knownElem);

            if (element is IComposableType<IIncomplete> incElem)
                return new ByRefType<IIncomplete, IMut>(incElem);

            return new ByRefType<IKnownOrNot, IMut>(element);
        }
    }

    static ITypeUsage<IKnownOrNot> CreateRichTuple(INamedTypeSymbol namedType, NullabilityKind nullability)
    {
        var elementsRaw = namedType.TupleElements.Select(te =>
        {
            var elemType = (IComposableType<IKnownOrNot>)Create(te.Type, te.NullableAnnotation);
            string? elemName = te.CorrespondingTupleField is not null ? te.Name : null;
            return (elemType, elemName);
        }).ToList();

        bool isIncomplete = elementsRaw.Any(e => e.elemType is IComposableType<IIncomplete>);

        var underlying = namedType.TupleUnderlyingType is not null
            ? CreateBoundGeneric(namedType.TupleUnderlyingType, NullabilityKind.NonNullable)
            : null;

        if (isIncomplete)
        {
            var elements = elementsRaw.Select(e => (ITupleElement<IIncomplete>)new TupleElement<IIncomplete>((IComposableType<IIncomplete>)e.elemType, e.elemName)).ToList();
            var underlyingStruct = (IBoundGenericType<IIncomplete, IStructType>)underlying!;
            return new RichTupleType<IIncomplete>(elements, underlyingStruct, nullability);
        }
        else
        {
            var elements = elementsRaw.Select(e => (ITupleElement<IKnown>)new TupleElement<IKnown>((IComposableType<IKnown>)e.elemType, e.elemName)).ToList();
            var underlyingStruct = (IBoundGenericType<IKnown, IStructType>)underlying!;
            return new RichTupleType<IKnown>(elements, underlyingStruct, nullability);
        }
    }

    static ITypeUsage<IKnownOrNot> CreateExplicitValueTuple(INamedTypeSymbol namedType, NullabilityKind nullability)
    {
        var boundGeneric = CreateBoundGeneric(namedType, NullabilityKind.NonNullable);
        bool isIncomplete = boundGeneric is IBoundGenericType<IIncomplete, IHasGenericParameters>;

        return isIncomplete
            ? (ITypeUsage<IKnownOrNot>)new ExplicitValueTupleType<IIncomplete>((IBoundGenericType<IIncomplete, IStructType>)boundGeneric, nullability)
            : (ITypeUsage<IKnownOrNot>)new ExplicitValueTupleType<IKnown>((IBoundGenericType<IKnown, IStructType>)boundGeneric, nullability);
    }

    static ITypeUsage<IKnownOrNot> CreateBoundGeneric(INamedTypeSymbol namedType, NullabilityKind nullability)
    {
        var typeArgs = namedType.TypeArguments
            .Select(ta => (IComposableType<IKnownOrNot>)Create(ta, ta.NullableAnnotation))
            .ToList();

        bool isIncomplete = typeArgs.Any(a => a is IComposableType<IIncomplete>);
        var decl = RectaleafRoslyn.FromSymbol(namedType.OriginalDefinition);

        return CreateBoundGenericType(decl, typeArgs, nullability, isIncomplete);
    }

    public static ITypeUsage<IKnownOrNot> CreateBoundGenericType(
        IDeclaredType decl,
        IReadOnlyList<IComposableType<IKnownOrNot>> typeArgs,
        NullabilityKind nullability,
        bool isIncomplete) => (decl, isIncomplete) switch
    {
        (IRegularClassType reg, true) => new BoundGenericType<IIncomplete, IRegularClassType>(reg, typeArgs, nullability),
        (IRegularClassType reg, false) => new BoundGenericType<IKnown, IRegularClassType>(reg, typeArgs, nullability),

        (IAbstractClassType abs, true) => new BoundGenericType<IIncomplete, IAbstractClassType>(abs, typeArgs, nullability),
        (IAbstractClassType abs, false) => new BoundGenericType<IKnown, IAbstractClassType>(abs, typeArgs, nullability),

        (IRecordClassType recCls, true) => new BoundGenericType<IIncomplete, IRecordClassType>(recCls, typeArgs, nullability),
        (IRecordClassType recCls, false) => new BoundGenericType<IKnown, IRecordClassType>(recCls, typeArgs, nullability),

        (IStructType str, true) => new BoundGenericType<IIncomplete, IStructType>(str, typeArgs, nullability),
        (IStructType str, false) => new BoundGenericType<IKnown, IStructType>(str, typeArgs, nullability),

        (IRecordStructType recStr, true) => new BoundGenericType<IIncomplete, IRecordStructType>(recStr, typeArgs, nullability),
        (IRecordStructType recStr, false) => new BoundGenericType<IKnown, IRecordStructType>(recStr, typeArgs, nullability),

        (IRefStructType refStr, true) => new BoundGenericType<IIncomplete, IRefStructType>(refStr, typeArgs, nullability),
        (IRefStructType refStr, false) => new BoundGenericType<IKnown, IRefStructType>(refStr, typeArgs, nullability),

        (IInterfaceType iface, true) => new BoundGenericType<IIncomplete, IInterfaceType>(iface, typeArgs, nullability),
        (IInterfaceType iface, false) => new BoundGenericType<IKnown, IInterfaceType>(iface, typeArgs, nullability),

        (IDelegateType del, true) => new BoundGenericType<IIncomplete, IDelegateType>(del, typeArgs, nullability),
        (IDelegateType del, false) => new BoundGenericType<IKnown, IDelegateType>(del, typeArgs, nullability),

        _ => throw new InvalidOperationException($"Type {decl.GetType().Name} cannot be used as a bound generic type definition.")
    };

    public static ITypeUsage<IKnown> CreatePlainType(IDeclaredType decl, NullabilityKind nullability) => decl switch
    {
        IRegularClassType reg => new PlainType<IRegularClassType>(reg, nullability),
        IAbstractClassType abs => new PlainType<IAbstractClassType>(abs, nullability),
        IRecordClassType recCls => new PlainType<IRecordClassType>(recCls, nullability),
        IStructType str => new PlainType<IStructType>(str, nullability),
        IRecordStructType recStr => new PlainType<IRecordStructType>(recStr, nullability),
        IRefStructType refStr => new PlainType<IRefStructType>(refStr, nullability),
        IEnumType enumType => new PlainType<IEnumType>(enumType, nullability),
        IInterfaceType iface => new PlainType<IInterfaceType>(iface, nullability),
        IDelegateType del => new PlainType<IDelegateType>(del, nullability),
        IInlineArrayType inlineArr => new PlainType<IInlineArrayType>(inlineArr, nullability),
        IStaticClassType stat => new PlainType<IStaticClassType>(stat, nullability),
        _ => throw new InvalidOperationException($"Unsupported declared type kind: {decl.GetType().Name}")
    };
}
