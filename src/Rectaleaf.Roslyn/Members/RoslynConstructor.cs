using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynConstructor(IMethodSymbol symbol) : IConstructor
{
    protected readonly IMethodSymbol symbol = symbol;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<ILocalFunction>? localFunctions;

    public abstract IDeclaredType DeclaringType { get; }

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<ILocalFunction> GetLocalFunctions()
        => localFunctions ??= RoslynLocalFunctionHelper.ExtractLocalFunctions(symbol, this);

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynInstanceConstructor(
    IMethodSymbol symbol,
    IHasInstanceConstructors? declaringType = null) : RoslynConstructor(symbol), IInstanceConstructor
{
    IHasInstanceConstructors? resolvedDeclaringType = declaringType;
    IReadOnlyList<IParameter>? parameters;
    bool? isPrimary;

    public override IDeclaredType DeclaringType
        => (resolvedDeclaringType ??= (RectaleafRoslyn.FromSymbol(symbol.ContainingType) as IHasInstanceConstructors)!);

    IHasInstanceConstructors IInstanceConstructor.DeclaringType
        => resolvedDeclaringType ??= (RectaleafRoslyn.FromSymbol(symbol.ContainingType) as IHasInstanceConstructors)!;

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public bool IsPrimary => isPrimary ??= CheckIsPrimary(symbol);

    public bool HasArglistParameter => symbol.IsVararg;

    public IReadOnlyList<IParameter> GetParameters()
        => parameters ??= symbol.Parameters.Select(p => (IParameter)new RoslynParameter(p)).ToList();

    static bool CheckIsPrimary(IMethodSymbol ctor)
    {
        foreach (var synRef in ctor.DeclaringSyntaxReferences)
        {
            var node = synRef.GetSyntax();
            if (node is TypeDeclarationSyntax)
                return true;
        }

        return false;
    }
}

internal sealed class RoslynStaticConstructor(
    IMethodSymbol symbol,
    IHasStaticConstructor? declaringType = null) : RoslynConstructor(symbol), IStaticConstructor
{
    IHasStaticConstructor? resolvedDeclaringType = declaringType;

    public override IDeclaredType DeclaringType
        => (resolvedDeclaringType ??= (RectaleafRoslyn.FromSymbol(symbol.ContainingType) as IHasStaticConstructor)!);

    IHasStaticConstructor IStaticConstructor.DeclaringType
        => resolvedDeclaringType ??= (RectaleafRoslyn.FromSymbol(symbol.ContainingType) as IHasStaticConstructor)!;
}
