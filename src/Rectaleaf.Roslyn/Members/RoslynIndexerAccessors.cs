using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynIndexerAccessor<TKind>(
    IMethodSymbol symbol,
    IIndexer<TKind, IAccess>? declaringIndexer = null) : IIndexerAccessor<TKind>
    where TKind : IKnownOrNot
{
    protected readonly IMethodSymbol symbol = symbol;
    IReturnInfo<TKind>? returnInfo;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<IParameter>? parameters;
    IReadOnlyList<ILocalFunction>? localFunctions;
    IIndexer<TKind, IAccess>? resolvedDeclaringIndexer = declaringIndexer;

    public IIndexer<TKind, IAccess> DeclaringIndexer => resolvedDeclaringIndexer ??= ResolveDeclaringIndexer();

    IIndexer<TKind, IAccess> ResolveDeclaringIndexer()
    {
        if (symbol.AssociatedSymbol is IPropertySymbol propSym && propSym.IsIndexer)
        {
            return (RectaleafRoslyn.FromIndexer(propSym) as IIndexer<TKind, IAccess>)!;
        }
        throw new InvalidOperationException($"Could not resolve declaring indexer for accessor {symbol.Name}");
    }

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IReturnInfo<TKind> ReturnInfo => returnInfo ??= new RoslynReturnInfo<TKind>(symbol);

    public bool IsReadOnly => symbol.IsReadOnly;

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<IParameter> GetParameters()
        => parameters ??= symbol.Parameters.Select(p => (IParameter)new RoslynParameter(p)).ToList();

    public IReadOnlyList<ILocalFunction> GetLocalFunctions()
        => localFunctions ??= RoslynLocalFunctionHelper.ExtractLocalFunctions(symbol, this);

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynIndexerGetAccessor<TKind>(
    IMethodSymbol symbol,
    IIndexer<TKind, IAccess>? declaringIndexer = null)
    : RoslynIndexerAccessor<TKind>(symbol, declaringIndexer), IIndexerGetAccessor<TKind>
    where TKind : IKnownOrNot;

internal sealed class RoslynIndexerSetAccessor<TKind>(
    IMethodSymbol symbol,
    IIndexer<TKind, IAccess>? declaringIndexer = null)
    : RoslynIndexerAccessor<TKind>(symbol, declaringIndexer), IIndexerSetAccessor<TKind>
    where TKind : IKnownOrNot;

internal sealed class RoslynIndexerInitAccessor<TKind>(
    IMethodSymbol symbol,
    IIndexer<TKind, IAccess>? declaringIndexer = null)
    : RoslynIndexerAccessor<TKind>(symbol, declaringIndexer), IIndexerInitAccessor<TKind>
    where TKind : IKnownOrNot;

internal sealed class RoslynIndexerGetSetAccessors(
    IIndexerGetAccessor<IKnownOrNot> getter,
    IIndexerSetAccessor<IKnownOrNot> setter) : IGetSet
{
    public IAccessor<IKnownOrNot> Getter => getter;
    public IAccessor<IKnownOrNot> Setter => setter;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynIndexerGetInitAccessors(
    IIndexerGetAccessor<IKnownOrNot> getter,
    IIndexerInitAccessor<IKnownOrNot> initAccessor) : IGetInit
{
    public IAccessor<IKnownOrNot> Getter => getter;
    public IAccessor<IKnownOrNot> InitAccessor => initAccessor;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynIndexerGetOnlyAccessors(
    IIndexerGetAccessor<IKnownOrNot> getter) : IGet
{
    public IAccessor<IKnownOrNot> Getter => getter;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynIndexerSetOnlyAccessors(
    IIndexerSetAccessor<IKnownOrNot> setter) : ISet
{
    public IAccessor<IKnownOrNot> Setter => setter;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynIndexerInitOnlyAccessors(
    IIndexerInitAccessor<IKnownOrNot> initAccessor) : IInit
{
    public IAccessor<IKnownOrNot> InitAccessor => initAccessor;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}
