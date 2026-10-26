using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynExtensionsBlock(
    INamedTypeSymbol symbol,
    IStaticClassType declaringType) : IExtensionsBlock
{
    protected readonly INamedTypeSymbol symbol = symbol;
    IReadOnlyList<IGenericParameter>? genericParameters;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<IMethod>? methods;
    IReadOnlyList<IProperty<IKnownOrNot, IAccess>>? properties;
    IReadOnlyList<IIndexer<IKnownOrNot, IAccess>>? indexers;

    public IStaticClassType DeclaringType => declaringType;

    public abstract ITypeUsage<IKnownOrNot> ExtendedType { get; }

    public bool IsGeneric => symbol.TypeParameters.Length > 0;

    public IReadOnlyList<IGenericParameter> GetGenericParameters()
        => genericParameters ??= symbol.TypeParameters
            .Select(tp => (IGenericParameter)new RoslynGenericParameter(tp, () => new GenericExtensionBlock(this)))
            .ToList();

    public IReadOnlyList<IAttribute> GetReceiverAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<IMethod> GetMethods()
        => methods ??= symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Select(m => m.IsStatic ? (IMethod)new RoslynStaticMethod(m, declaringType) : new RoslynInstanceMethod(m, declaringType))
            .ToList();

    public IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties()
        => properties ??= symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsIndexer)
            .Select(p => RoslynProperty<IKnownOrNot, IAccess>.Create(p, declaringType))
            .ToList();

    public IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers()
        => indexers ??= symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsIndexer)
            .Select(p => RoslynIndexer<IKnownOrNot, IAccess>.Create(p, declaringType as IHasIndexers))
            .ToList();

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();

    public static IExtensionsBlock Create(INamedTypeSymbol symbol, IStaticClassType declaringType)
    {
        var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
        bool isUnnamed = false;

        if (syntax is ExtensionBlockDeclarationSyntax eb)
        {
            var p = eb.ParameterList?.Parameters.FirstOrDefault();
            if (p is not null && (string.IsNullOrEmpty(p.Identifier.Text) || p.Identifier.RawKind == 0))
                isUnnamed = true;
        }

        return isUnnamed
            ? new RoslynStaticExtensionsBlock(symbol, declaringType)
            : new RoslynInstanceExtensionsBlock(symbol, declaringType);
    }
}

internal sealed class RoslynInstanceExtensionsBlock(
    INamedTypeSymbol symbol,
    IStaticClassType declaringType) : RoslynExtensionsBlock(symbol, declaringType), IInstanceExtensionsBlock
{
    IParameter? receiver;

    public IParameter Receiver => receiver ??= ResolveReceiver();

    public override ITypeUsage<IKnownOrNot> ExtendedType => Receiver.Type;

    IParameter ResolveReceiver()
    {
        var synRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        var syntax = synRef?.GetSyntax() as ExtensionBlockDeclarationSyntax;
        var pSyntax = syntax?.ParameterList?.Parameters.FirstOrDefault();

        if (synRef is not null && pSyntax?.Type is not null)
        {
            var compilation = RoslynLocalFunctionHelper.GetCompilation(symbol);
            if (compilation is not null)
            {
                var model = compilation.GetSemanticModel(synRef.SyntaxTree);
                if (model.GetDeclaredSymbol(pSyntax) is IParameterSymbol paramSymbol)
                    return new RoslynParameter(paramSymbol);

                var typeInfo = model.GetTypeInfo(pSyntax.Type);
                if (typeInfo.Type is not null)
                    return new RoslynSyntheticParameter(pSyntax.Identifier.Text, typeInfo.Type);
            }
        }

        var firstMethod = symbol.GetMembers().OfType<IMethodSymbol>().FirstOrDefault();
        if (firstMethod is not null && firstMethod.Parameters.Length > 0 && firstMethod.Parameters[0].IsThis)
            return new RoslynParameter(firstMethod.Parameters[0]);

        if (symbol.TypeArguments.Length > 0)
            return new RoslynSyntheticParameter(pSyntax?.Identifier.Text ?? "receiver", symbol.TypeArguments[0]);

        throw new InvalidOperationException($"Cannot resolve receiver for instance extension block '{symbol.Name}'.");
    }
}

internal sealed class RoslynStaticExtensionsBlock(
    INamedTypeSymbol symbol,
    IStaticClassType declaringType) : RoslynExtensionsBlock(symbol, declaringType), IStaticExtensionsBlock
{
    ITypeUsage<IKnownOrNot>? extendedType;

    public override ITypeUsage<IKnownOrNot> ExtendedType => extendedType ??= ResolveExtendedType();

    ITypeUsage<IKnownOrNot> ResolveExtendedType()
    {
        var synRef = symbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (synRef?.GetSyntax() is ExtensionBlockDeclarationSyntax eb)
        {
            var p = eb.ParameterList?.Parameters.FirstOrDefault();
            if (p?.Type is not null)
            {
                var compilation = RoslynLocalFunctionHelper.GetCompilation(symbol);
                if (compilation is not null)
                {
                    var model = compilation.GetSemanticModel(synRef.SyntaxTree);
                    var typeInfo = model.GetTypeInfo(p.Type);
                    if (typeInfo.Type is not null)
                        return RoslynTypeUsageFactory.Create(typeInfo.Type);
                }
            }
        }

        if (symbol.TypeArguments.Length > 0)
            return RoslynTypeUsageFactory.Create(symbol.TypeArguments[0]);

        throw new InvalidOperationException($"Cannot resolve extended type for static extension block '{symbol.Name}'.");
    }
}

internal sealed class RoslynSyntheticParameter(string name, ITypeSymbol typeSymbol) : IParameter
{
    ITypeUsage<IKnownOrNot>? type;

    public string Name => name;

    public ITypeUsage<IKnownOrNot> Type => type ??= RoslynTypeUsageFactory.Create(typeSymbol);

    public ParameterModifiers Modifiers => ParameterModifiers.None;

    public bool TryGetDefaultValue(out object? defaultValue)
    {
        defaultValue = null;
        return false;
    }

    public IReadOnlyList<IAttribute> GetAttributes() => Array.Empty<IAttribute>();

    public override bool Equals(object? obj)
        => obj is IParameter other && ParameterEqualityComparer.Default.Equals(this, other);

    public override int GetHashCode()
        => ParameterEqualityComparer.Default.GetHashCode(this);
}
