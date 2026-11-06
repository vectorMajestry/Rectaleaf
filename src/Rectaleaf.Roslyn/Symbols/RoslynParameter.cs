using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Symbols;

internal sealed class RoslynParameter(IParameterSymbol symbol) : IParameter
{
    ITypeUsage<IKnownOrNot>? type;
    IReadOnlyList<IAttribute>? attributes;

    public string Name => symbol.Name;

    public ITypeUsage<IKnownOrNot> Type => type ??= ResolveType();

    ITypeUsage<IKnownOrNot> ResolveType()
    {
        bool isSynthesizedRecordParam = symbol.ContainingSymbol is IMethodSymbol
        {
            IsImplicitlyDeclared: true,
            ContainingType.IsRecord: true
        };

        ITypeUsage<IKnownOrNot> rawUsage = RoslynTypeUsageFactory.Create(
            symbol.Type,
            symbol.NullableAnnotation,
            RoslynTypeUsageFactory.ExtractTypeSyntax(symbol),
            defaultNonNullable: isSynthesizedRecordParam);

        if (rawUsage is IComposableType<IKnownOrNot> composable)
        {
            return symbol.RefKind switch
            {
                RefKind.Ref or RefKind.Out => RoslynTypeUsageFactory.CreateByRef(composable, isReadOnly: false),
                RefKind.In or RefKind.RefReadOnly or RefKind.RefReadOnlyParameter => RoslynTypeUsageFactory.CreateByRef(composable, isReadOnly: true),
                _ => rawUsage
            };
        }

        return rawUsage;
    }

    public ParameterModifiers Modifiers => ComputeModifiers(symbol);

    public bool TryGetDefaultValue(out object? defaultValue)
    {
        if (symbol.HasExplicitDefaultValue)
        {
            defaultValue = symbol.ExplicitDefaultValue;
            return true;
        }

        defaultValue = null;
        return false;
    }

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public override bool Equals(object? obj)
        => obj is IParameter other && ParameterEqualityComparer.Default.Equals(this, other);

    public override int GetHashCode()
        => ParameterEqualityComparer.Default.GetHashCode(this);

    static ParameterModifiers ComputeModifiers(IParameterSymbol param)
    {
        var modifiers = ParameterModifiers.None;

        if (param.IsThis)
            modifiers |= ParameterModifiers.This;

        if (param.IsParams)
            modifiers |= ParameterModifiers.Params;

        if (param.ScopedKind is ScopedKind.ScopedRef or ScopedKind.ScopedValue)
        {
            if (param.RefKind != RefKind.Out || HasExplicitScoped(param))
                modifiers |= ParameterModifiers.Scoped;
        }

        if (param.RefKind == RefKind.Ref)
            modifiers |= ParameterModifiers.Ref;
        else if (param.RefKind == RefKind.Out)
            modifiers |= ParameterModifiers.Out;
        else if (param.RefKind == RefKind.RefReadOnlyParameter)
            modifiers |= ParameterModifiers.RefReadOnly;
        else if (param.RefKind == RefKind.In)
        {
            bool isRefReadOnly = false;
            foreach (SyntaxReference synRef in param.DeclaringSyntaxReferences)
            {
                if (synRef.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax pSyntax)
                {
                    if (pSyntax.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.RefKeyword))
                    {
                        isRefReadOnly = true;
                        break;
                    }
                }
            }
            modifiers |= isRefReadOnly ? ParameterModifiers.RefReadOnly : ParameterModifiers.In;
        }

        return modifiers;
    }

    static bool HasExplicitScoped(IParameterSymbol param)
    {
        if (param.DeclaringSyntaxReferences.Length > 0)
        {
            foreach (SyntaxReference synRef in param.DeclaringSyntaxReferences)
            {
                SyntaxNode node = synRef.GetSyntax();
                if (node is Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax pSyntax &&
                    pSyntax.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ScopedKeyword))
                {
                    return true;
                }
                if (node is Microsoft.CodeAnalysis.CSharp.Syntax.FunctionPointerParameterSyntax fpSyntax &&
                    fpSyntax.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ScopedKeyword))
                {
                    return true;
                }
            }
            return false;
        }

        foreach (AttributeData attr in param.GetAttributes())
        {
            if (attr.AttributeClass?.Name == "ScopedRefAttribute")
                return true;
        }

        return false;
    }
}
