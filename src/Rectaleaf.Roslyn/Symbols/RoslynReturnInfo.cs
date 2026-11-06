using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Symbols;

internal sealed class RoslynReturnInfo<TKind> : IReturnInfo<TKind>
    where TKind : IKnownOrNot
{
    readonly IMethodSymbol? methodSymbol;
    readonly ImmutableArray<AttributeData> returnAttributes;
    ITypeUsage<TKind>? type;
    IReadOnlyList<IAttribute>? attributes;

    public RoslynReturnInfo(IMethodSymbol method)
    {
        methodSymbol = method;
        returnAttributes = method.GetReturnTypeAttributes();
    }

    public RoslynReturnInfo(ITypeUsage<TKind> type, ImmutableArray<AttributeData> returnAttributes)
    {
        this.type = type;
        this.returnAttributes = returnAttributes;
    }

    public RoslynReturnInfo(ITypeUsage<TKind> type, IReadOnlyList<IAttribute> attributes)
    {
        this.type = type;
        this.attributes = attributes;
        returnAttributes = ImmutableArray<AttributeData>.Empty;
    }

    public ITypeUsage<TKind> Type => type ??= ResolveMethodReturnType();

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(returnAttributes);

    ITypeUsage<TKind> ResolveMethodReturnType()
    {
        if (methodSymbol is null)
            throw new InvalidOperationException("Type must be provided when method symbol is null.");

        var rawType = RoslynTypeUsageFactory.Create(
            methodSymbol.ReturnType,
            methodSymbol.ReturnNullableAnnotation,
            RoslynTypeUsageFactory.ExtractReturnTypeSyntax(methodSymbol));

        if (rawType is not IComposableType<TKind> composable)
            return (ITypeUsage<TKind>)rawType;

        return methodSymbol.RefKind switch
        {
            RefKind.Ref => (ITypeUsage<TKind>)(object)new ByRefType<TKind, IMut>(composable),
            RefKind.RefReadOnly or RefKind.In => (ITypeUsage<TKind>)(object)new ByRefType<TKind, IReadOnly>(composable),
            _ => (ITypeUsage<TKind>)rawType
        };
    }

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();
}

