using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;
using System.Reflection.Metadata;

namespace Rectaleaf.Roslyn.TypeUsages;

internal static class RoslynTypeUsageFactory
{
    public static TypeSyntax? ExtractTypeSyntax(ISymbol symbol)
    {
        foreach (SyntaxReference synRef in symbol.DeclaringSyntaxReferences)
        {
            SyntaxNode node = synRef.GetSyntax();
            TypeSyntax? found = node switch
            {
                VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax vds } => vds.Type,
                PropertyDeclarationSyntax pds => pds.Type,
                IndexerDeclarationSyntax ids => ids.Type,
                EventDeclarationSyntax eds => eds.Type,
                ParameterSyntax ps => ps.Type,
                FunctionPointerParameterSyntax fpps => fpps.Type,
                _ => null
            };
            if (found is not null)
                return found;
        }

        return null;
    }

    public static TypeSyntax? ExtractReturnTypeSyntax(IMethodSymbol method)
    {
        if (method.AssociatedSymbol is IPropertySymbol prop)
            return ExtractTypeSyntax(prop);

        if (method.MethodKind == MethodKind.DelegateInvoke && method.ContainingType is not null)
        {
            foreach (SyntaxReference synRef in method.ContainingType.DeclaringSyntaxReferences)
            {
                if (synRef.GetSyntax() is DelegateDeclarationSyntax dds)
                    return dds.ReturnType;
            }
        }

        foreach (SyntaxReference synRef in method.DeclaringSyntaxReferences)
        {
            SyntaxNode node = synRef.GetSyntax();
            TypeSyntax? found = node switch
            {
                MethodDeclarationSyntax mds => mds.ReturnType,
                LocalFunctionStatementSyntax lfs => lfs.ReturnType,
                OperatorDeclarationSyntax ods => ods.ReturnType,
                ConversionOperatorDeclarationSyntax cods => cods.Type,
                DelegateDeclarationSyntax dds => dds.ReturnType,
                _ => null
            };
            if (found is not null)
                return found;
        }

        return null;
    }

    public static ITypeUsage<IKnownOrNot> Create(
        ITypeSymbol symbol,
        NullableAnnotation nullableAnnotation = NullableAnnotation.None,
        TypeSyntax? syntax = null,
        bool isNullableValueType = false,
        bool defaultNonNullable = false)
    {
        if (symbol is null)
            throw new ArgumentNullException(nameof(symbol));

        if (syntax is RefTypeSyntax rts)
            syntax = rts.Type;

        // Handle System.Nullable<T>
        if (symbol is INamedTypeSymbol namedNullable && namedNullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            ITypeSymbol innerType = namedNullable.TypeArguments[0];
            TypeSyntax? innerSyntax = syntax switch
            {
                NullableTypeSyntax nts => nts.ElementType,
                GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } gns => gns.TypeArgumentList.Arguments[0],
                QualifiedNameSyntax { Right: GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } qgns } => qgns.TypeArgumentList.Arguments[0],
                _ => null
            };
            return Create(innerType, NullableAnnotation.Annotated, innerSyntax, isNullableValueType: true, defaultNonNullable: defaultNonNullable);
        }

        if (syntax is NullableTypeSyntax refNts)
            syntax = refNts.ElementType;

        NullableAnnotation effectiveAnnotation = nullableAnnotation != NullableAnnotation.None
            ? nullableAnnotation
            : symbol.NullableAnnotation;

        if (effectiveAnnotation == NullableAnnotation.None && defaultNonNullable)
            effectiveAnnotation = NullableAnnotation.NotAnnotated;

        NullabilityKind nullability = RoslynSymbolMapping.ToNullabilityKind(effectiveAnnotation);

        if (!isNullableValueType && symbol.IsValueType && symbol.TypeKind != TypeKind.TypeParameter)
            nullability = NullabilityKind.NonNullable;

        // Special / Predefined types
        if (symbol.SpecialType == SpecialType.System_Void)
            return new PredefinedType(PredefinedTypeKind.Void, NullabilityKind.NonNullable);

        PredefinedTypeKind? predefinedKind = RoslynSymbolMapping.ToPredefinedTypeKind(symbol.SpecialType);
        if (predefinedKind.HasValue)
            return new PredefinedType(predefinedKind.Value, nullability);

        // Generic type parameter (placeholder)
        if (symbol is ITypeParameterSymbol typeParam)
            return new PlaceholderType(new RoslynGenericParameter(typeParam), nullability);

        // Array
        if (symbol is IArrayTypeSymbol arraySymbol)
            return CreateArray(arraySymbol, nullability, syntax, defaultNonNullable);

        // Pointer
        if (symbol is IPointerTypeSymbol pointerSymbol)
            return CreatePointer(pointerSymbol, syntax, defaultNonNullable);

        // Function Pointer
        if (symbol is IFunctionPointerTypeSymbol fpSymbol)
            return CreateFunctionPointer(fpSymbol);

        // Named types: Tuples, Bound Generics, Plain Types
        if (symbol is INamedTypeSymbol namedType)
        {
            if (namedType.IsTupleType)
            {
                bool isExplicitValueTuple = syntax is not null and not TupleTypeSyntax;
                return isExplicitValueTuple
                    ? CreateExplicitValueTuple(namedType, nullability, syntax, defaultNonNullable)
                    : CreateRichTuple(namedType, nullability, syntax as TupleTypeSyntax, defaultNonNullable);
            }

            if (namedType.Name == "ValueTuple" && namedType.ContainingNamespace?.ToDisplayString() == "System" && namedType.TypeArguments.Length > 0)
                return CreateExplicitValueTuple(namedType, nullability, syntax, defaultNonNullable);

            if (namedType.IsGenericType && !namedType.IsUnboundGenericType && namedType.TypeArguments.Length > 0)
                return CreateBoundGeneric(namedType, nullability, syntax, defaultNonNullable);

            IDeclaredType decl = RectaleafRoslyn.FromTypeDefinition(namedType.IsUnboundGenericType ? namedType.OriginalDefinition : namedType);
            return CreatePlainType(decl, nullability);
        }

        throw new NotSupportedException($"Unsupported Roslyn type symbol: {symbol} (Kind: {symbol.TypeKind})");
    }

    static ITypeUsage<IKnownOrNot> CreateArray(IArrayTypeSymbol arraySymbol, NullabilityKind nullability, TypeSyntax? syntax, bool defaultNonNullable)
    {
        TypeSyntax? elemSyntax = syntax is ArrayTypeSyntax ats && ats.RankSpecifiers.Count <= 1 ? ats.ElementType : null;
        var elemUsage = (IComposableType<IKnownOrNot>)Create(arraySymbol.ElementType, arraySymbol.ElementNullableAnnotation, elemSyntax, defaultNonNullable: defaultNonNullable);

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

    static ITypeUsage<IKnownOrNot> CreatePointer(IPointerTypeSymbol pointerSymbol, TypeSyntax? syntax, bool defaultNonNullable)
    {
        TypeSyntax? elemSyntax = syntax is PointerTypeSyntax pts ? pts.ElementType : null;
        var elemUsage = (IComposableType<IKnownOrNot>)Create(pointerSymbol.PointedAtType, pointerSymbol.PointedAtType.NullableAnnotation, elemSyntax, defaultNonNullable: defaultNonNullable);

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
                RefKind.Ref => CreateByRef(composable, isReadOnly: false),
                RefKind.RefReadOnly or RefKind.In => CreateByRef(composable, isReadOnly: true),
                _ => rawReturnType
            };
        }

        if (fpSymbol.Signature.CallingConvention != SignatureCallingConvention.Default)
        {
            IReadOnlyList<CallConvKind> convs = fpSymbol.Signature.CallingConvention switch
            {
                SignatureCallingConvention.CDecl => [CallConvKind.Cdecl],
                SignatureCallingConvention.StdCall => [CallConvKind.Stdcall],
                SignatureCallingConvention.ThisCall => [CallConvKind.Thiscall],
                SignatureCallingConvention.FastCall => [CallConvKind.Fastcall],
                _ => fpSymbol.Signature.UnmanagedCallingConventionTypes
                    .Select(t => RoslynSymbolMapping.ToCallConvKind(t.Name))
                    .Where(k => k.HasValue)
                    .Select(k => k!.Value)
                    .ToList()
            };

            CallConvFlags flags = RoslynSymbolMapping.ToCallConvFlags(convs);

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

    internal static ITypeUsage<IKnownOrNot> CreateByRef(IComposableType<IKnownOrNot> element, bool isReadOnly)
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

    static ITypeUsage<IKnownOrNot> CreateRichTuple(INamedTypeSymbol namedType, NullabilityKind nullability, TupleTypeSyntax? tupleSyntax, bool defaultNonNullable)
    {
        var elementsRaw = namedType.TupleElements.Select((te, idx) =>
        {
            TypeSyntax? elemSyntax = tupleSyntax is not null && idx < tupleSyntax.Elements.Count
                ? tupleSyntax.Elements[idx].Type
                : null;
            var elemType = (IComposableType<IKnownOrNot>)Create(te.Type, te.NullableAnnotation, elemSyntax, defaultNonNullable: defaultNonNullable);
            string? elemName = te.IsExplicitlyNamedTupleElement ? te.Name : null;
            return (elemType, elemName);
        }).ToList();

        bool isIncomplete = elementsRaw.Any(e => e.elemType is IComposableType<IIncomplete>);

        var underlying = namedType.TupleUnderlyingType is not null
            ? CreateBoundGeneric(namedType.TupleUnderlyingType, NullabilityKind.NonNullable, syntax: null, defaultNonNullable: defaultNonNullable)
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

    static ITypeUsage<IKnownOrNot> CreateExplicitValueTuple(INamedTypeSymbol namedType, NullabilityKind nullability, TypeSyntax? syntax, bool defaultNonNullable)
    {
        INamedTypeSymbol underlyingTuple = namedType.TupleUnderlyingType ?? namedType;
        var boundGeneric = CreateBoundGeneric(underlyingTuple, NullabilityKind.NonNullable, syntax, defaultNonNullable);
        bool isIncomplete = boundGeneric is IBoundGenericType<IIncomplete, IHasGenericParameters>;

        return isIncomplete
            ? (ITypeUsage<IKnownOrNot>)new ExplicitValueTupleType<IIncomplete>((IBoundGenericType<IIncomplete, IStructType>)boundGeneric, nullability)
            : (ITypeUsage<IKnownOrNot>)new ExplicitValueTupleType<IKnown>((IBoundGenericType<IKnown, IStructType>)boundGeneric, nullability);
    }

    static ITypeUsage<IKnownOrNot> CreateBoundGeneric(INamedTypeSymbol namedType, NullabilityKind nullability, TypeSyntax? syntax, bool defaultNonNullable)
    {
        SeparatedSyntaxList<TypeSyntax>? typeArgSyntaxes = syntax switch
        {
            GenericNameSyntax gns => gns.TypeArgumentList.Arguments,
            QualifiedNameSyntax { Right: GenericNameSyntax qgns } => qgns.TypeArgumentList.Arguments,
            AliasQualifiedNameSyntax { Name: GenericNameSyntax agns } => agns.TypeArgumentList.Arguments,
            _ => null
        };

        var typeArgs = new List<IComposableType<IKnownOrNot>>(namedType.TypeArguments.Length);
        for (int i = 0; i < namedType.TypeArguments.Length; i++)
        {
            ITypeSymbol ta = namedType.TypeArguments[i];
            NullableAnnotation taAnnotation = i < namedType.TypeArgumentNullableAnnotations.Length && namedType.TypeArgumentNullableAnnotations[i] != NullableAnnotation.None
                ? namedType.TypeArgumentNullableAnnotations[i]
                : ta.NullableAnnotation;
            TypeSyntax? taSyntax = typeArgSyntaxes.HasValue && i < typeArgSyntaxes.Value.Count
                ? typeArgSyntaxes.Value[i]
                : null;
            typeArgs.Add((IComposableType<IKnownOrNot>)Create(ta, taAnnotation, taSyntax, defaultNonNullable: defaultNonNullable));
        }

        bool isIncomplete = typeArgs.Any(a => a is IComposableType<IIncomplete>);
        IDeclaredType decl = RectaleafRoslyn.FromTypeDefinition(namedType.OriginalDefinition);

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
