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

namespace Rectaleaf.Roslyn;

public static class RectaleafRoslyn
{
    public static IDeclaredType FromTypeDefinition(INamedTypeSymbol symbol)
        => (symbol ?? throw new ArgumentNullException(nameof(symbol)))
            .TypeKind switch
        {
            TypeKind.Error
                => throw new ArgumentException("Error type symbols are not supported.", nameof(symbol)),

            TypeKind.Pointer or TypeKind.Array or TypeKind.FunctionPointer or TypeKind.TypeParameter
                => throw new ArgumentException(
                    $"Expected nominal type definition, but received type usage: {symbol.TypeKind}", nameof(symbol)),

            _ when symbol.IsGenericType && !symbol.IsDefinition
                => throw new ArgumentException(
                    $"Expected nominal type definition, but received generic type usage.", nameof(symbol)),

            TypeKind.Delegate => new RoslynDelegateType(symbol),
            TypeKind.Enum => RoslynEnumType.Create(symbol),
            TypeKind.Interface => new RoslynInterfaceType(symbol),
            TypeKind.Struct => ResolveStructDefinition(symbol),
            TypeKind.Class => ResolveClassDefinition(symbol),

            _ => throw new NotSupportedException(
                $"Unsupported declared type symbol: {symbol} (TypeKind: {symbol.TypeKind})")
        };

    static IDeclaredType ResolveStructDefinition(INamedTypeSymbol structSymbol)
        => structSymbol switch
        {
            _ when structSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "InlineArrayAttribute")
                => new RoslynInlineArrayType(structSymbol),
            { IsRecord: true } => new RoslynRecordStructType(structSymbol),
            { IsRefLikeType: true } => new RoslynRefStructType(structSymbol),
            _ => new RoslynStructType(structSymbol),
        };

    static IDeclaredType ResolveClassDefinition(INamedTypeSymbol classSymbol)
        => classSymbol switch
        {
            { IsStatic: true } => new RoslynStaticClassType(classSymbol),
            { IsRecord: true } => new RoslynRecordClassType(classSymbol),
            { IsAbstract: true } => new RoslynAbstractClassType(classSymbol),
            _ => new RoslynRegularClassType(classSymbol),
        };

    public static IMethod FromMethod(IMethodSymbol method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));

        return method.IsStatic
            ? new RoslynStaticMethod(method)
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
            ? new RoslynStaticConstructor(constructor)
            : new RoslynInstanceConstructor(constructor);
    }

    public static IOperator FromOperator(IMethodSymbol @operator)
    {
        if (@operator is null)
            throw new ArgumentNullException(nameof(@operator));

        return RoslynOperator.Create(@operator)
            ?? throw new ArgumentException("The method symbol is not a user-defined operator or conversion operator.", nameof(@operator));
    }
}
