using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Events;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.DeclaredTypes;
using Rectaleaf.Roslyn.Members;
using Rectaleaf.Roslyn.Symbols;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn;

public static class RectaleafRoslyn
{
    public static IDeclaredType FromSymbol(INamedTypeSymbol symbol)
    {
        if (symbol is null)
            throw new ArgumentNullException(nameof(symbol));

        if (symbol.TypeKind == TypeKind.Error)
            throw new ArgumentException("Error type symbols are not supported.", nameof(symbol));

        if (symbol.TypeKind is TypeKind.Pointer or TypeKind.Array or TypeKind.FunctionPointer or TypeKind.TypeParameter)
            throw new ArgumentException($"Expected nominal type definition, but received type usage: {symbol.TypeKind}", nameof(symbol));

        if (symbol.TypeKind == TypeKind.Delegate)
            return new RoslynDelegateType(symbol);

        if (symbol.TypeKind == TypeKind.Enum)
            return RoslynEnumType.Create(symbol);

        if (symbol.TypeKind == TypeKind.Interface)
            return new RoslynInterfaceType(symbol);

        if (symbol.TypeKind == TypeKind.Struct)
        {
            if (symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "InlineArrayAttribute"))
                return new RoslynInlineArrayType(symbol);

            if (symbol.IsRecord)
                return new RoslynRecordStructType(symbol);

            if (symbol.IsRefLikeType)
                return new RoslynRefStructType(symbol);

            return new RoslynStructType(symbol);
        }

        if (symbol.TypeKind == TypeKind.Class)
        {
            if (symbol.IsStatic)
                return new RoslynStaticClassType(symbol);

            if (symbol.IsRecord)
                return new RoslynRecordClassType(symbol);

            if (symbol.IsAbstract)
                return new RoslynAbstractClassType(symbol);

            return new RoslynRegularClassType(symbol);
        }

        throw new NotSupportedException($"Unsupported declared type symbol: {symbol} (TypeKind: {symbol.TypeKind})");
    }

    public static IMethod FromMethod(IMethodSymbol method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));

        return method.IsStatic
            ? (IMethod)new RoslynStaticMethod(method)
            : new RoslynInstanceMethod(method);
    }

    public static IField<IKnownOrNot> FromField(IFieldSymbol field)
    {
        if (field is null)
            throw new ArgumentNullException(nameof(field));

        return RoslynField<IKnownOrNot>.Create(field);
    }

    public static IProperty<IKnownOrNot, IAccess> FromProperty(IPropertySymbol property)
    {
        if (property is null)
            throw new ArgumentNullException(nameof(property));

        if (property.IsIndexer)
            throw new ArgumentException("Property symbol represents an indexer. Use FromIndexer instead.", nameof(property));

        return RoslynProperty<IKnownOrNot, IAccess>.Create(property);
    }

    public static IIndexer<IKnownOrNot, IAccess> FromIndexer(IPropertySymbol indexer)
    {
        if (indexer is null)
            throw new ArgumentNullException(nameof(indexer));

        if (!indexer.IsIndexer)
            throw new ArgumentException("Property symbol does not represent an indexer. Use FromProperty instead.", nameof(indexer));

        return RoslynIndexer<IKnownOrNot, IAccess>.Create(indexer);
    }

    public static IEvent<IKnownOrNot> FromEvent(IEventSymbol @event)
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        return RoslynEvent.Create(@event);
    }

    public static IParameter FromParameter(IParameterSymbol parameter)
    {
        if (parameter is null)
            throw new ArgumentNullException(nameof(parameter));

        return new RoslynParameter(parameter);
    }

    public static IGenericParameter FromGenericParameter(ITypeParameterSymbol genericParameter)
    {
        if (genericParameter is null)
            throw new ArgumentNullException(nameof(genericParameter));

        return new RoslynGenericParameter(genericParameter);
    }

    public static IConstructor FromConstructor(IMethodSymbol constructor)
    {
        if (constructor is null)
            throw new ArgumentNullException(nameof(constructor));

        return constructor.IsStatic
            ? (IConstructor)new RoslynStaticConstructor(constructor)
            : new RoslynInstanceConstructor(constructor);
    }

    public static IOperator FromOperator(IMethodSymbol @operator)
    {
        if (@operator is null)
            throw new ArgumentNullException(nameof(@operator));

        return RoslynOperator.Create(@operator)
            ?? throw new ArgumentException("The method symbol is not a user-defined operator or conversion operator.", nameof(@operator));
    }

    public static ITypeUsage<IKnownOrNot> FromTypeUsage(ITypeSymbol symbol, NullableAnnotation nullableAnnotation = NullableAnnotation.None)
    {
        if (symbol is null)
            throw new ArgumentNullException(nameof(symbol));

        return RoslynTypeUsageFactory.Create(symbol, nullableAnnotation);
    }
}
