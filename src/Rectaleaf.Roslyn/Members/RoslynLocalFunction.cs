using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.Members;

internal sealed class RoslynLocalFunction(
    IMethodSymbol symbol,
    LocalFunctionStatementSyntax syntaxNode,
    Compilation compilation,
    IHasLocalFunctions declaringEntity,
    int ordinal) : ILocalFunction
{
    IReturnInfo<IKnownOrNot>? returnInfo;
    IReadOnlyList<IParameter>? parameters;
    IReadOnlyList<IGenericParameter>? genericParameters;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<ILocalFunction>? localFunctions;

    public string Name => symbol.Name;

    public int Ordinal => ordinal;

    public IReturnInfo<IKnownOrNot> ReturnInfo => returnInfo ??= new RoslynReturnInfo<IKnownOrNot>(symbol);

    public IHasLocalFunctions DeclaringEntity => declaringEntity;

    public bool IsStatic => symbol.IsStatic;

    public bool IsGeneric => symbol.IsGenericMethod;

    public IReadOnlyList<IParameter> GetParameters()
        => parameters ??= symbol.Parameters.Select(p => (IParameter)new RoslynParameter(p)).ToList();

    public IReadOnlyList<IGenericParameter> GetGenericParameters()
        => genericParameters ??= symbol.TypeParameters
            .Select(tp => (IGenericParameter)new RoslynGenericParameter(tp, () => new GenericLocalFunction(this)))
            .ToList();

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<ILocalFunction> GetLocalFunctions()
        => localFunctions ??= RoslynLocalFunctionHelper.ExtractNestedLocalFunctions(syntaxNode, compilation, this);

    public override bool Equals(object? obj)
        => obj is ILocalFunction other && LocalFunctionEqualityComparer.Default.Equals(this, other);

    public override int GetHashCode()
        => LocalFunctionEqualityComparer.Default.GetHashCode(this);
}

internal static class RoslynLocalFunctionHelper
{
    public static IReadOnlyList<ILocalFunction> ExtractLocalFunctions(ISymbol symbol, IHasLocalFunctions declaringEntity)
    {
        if (symbol.DeclaringSyntaxReferences.IsEmpty)
            return Array.Empty<ILocalFunction>();

        var compilation = GetCompilation(symbol);
        if (compilation is null)
            return Array.Empty<ILocalFunction>();

        var results = new List<ILocalFunction>();
        int ordinal = 0;

        foreach (var synRef in symbol.DeclaringSyntaxReferences)
        {
            var node = synRef.GetSyntax();
            var model = compilation.GetSemanticModel(synRef.SyntaxTree);
            var localNodes = new List<LocalFunctionStatementSyntax>();
            CollectDirectLocalFunctions(node, localNodes);

            foreach (var localNode in localNodes)
            {
                if (model.GetDeclaredSymbol(localNode) is IMethodSymbol localSymbol)
                    results.Add(new RoslynLocalFunction(localSymbol, localNode, compilation, declaringEntity, ordinal++));
            }
        }

        return results;
    }

    public static IReadOnlyList<ILocalFunction> ExtractNestedLocalFunctions(LocalFunctionStatementSyntax parentNode, Compilation compilation, IHasLocalFunctions declaringEntity)
    {
        var model = compilation.GetSemanticModel(parentNode.SyntaxTree);
        var localNodes = new List<LocalFunctionStatementSyntax>();
        CollectDirectLocalFunctions(parentNode.Body ?? (SyntaxNode?)parentNode.ExpressionBody, localNodes);

        if (localNodes.Count == 0)
            return Array.Empty<ILocalFunction>();

        var results = new List<ILocalFunction>(localNodes.Count);
        int ordinal = 0;
        foreach (var localNode in localNodes)
        {
            if (model.GetDeclaredSymbol(localNode) is IMethodSymbol localSymbol)
                results.Add(new RoslynLocalFunction(localSymbol, localNode, compilation, declaringEntity, ordinal++));
        }

        return results;
    }

    static void CollectDirectLocalFunctions(SyntaxNode? parent, List<LocalFunctionStatementSyntax> results)
    {
        if (parent is null)
            return;

        foreach (var child in parent.ChildNodes())
        {
            if (child is LocalFunctionStatementSyntax localFunc)
                results.Add(localFunc);
            else
                CollectDirectLocalFunctions(child, results);
        }
    }

    internal static Compilation? GetCompilation(ISymbol symbol)
    {
        var asm = symbol.ContainingAssembly;
        if (asm is null)
            return null;

        var type = asm.GetType();
        while (type is not null && type != typeof(object))
        {
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.Name.EndsWith("Compilation", StringComparison.Ordinal) && typeof(Compilation).IsAssignableFrom(prop.PropertyType))
                {
                    if (prop.GetValue(asm) is Compilation comp)
                        return comp;
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.Name.EndsWith("compilation", StringComparison.OrdinalIgnoreCase) && typeof(Compilation).IsAssignableFrom(field.FieldType))
                {
                    if (field.GetValue(asm) is Compilation comp)
                        return comp;
                }
            }

            type = type.BaseType;
        }

        return null;
    }

}

