using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynIndexer<TKind, TAccess>(
    IPropertySymbol symbol,
    TAccess accessors,
    IHasIndexers? declaringType = null) : IIndexer<TKind, TAccess>
    where TKind : IKnownOrNot
    where TAccess : IAccess
{
    protected readonly IPropertySymbol symbol = symbol;
    IHasIndexers? resolvedDeclaringType = declaringType;
    IInheritanceKind<IIndexer<TKind, TAccess>>? inheritance;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<IParameter>? parameters;

    public string Name => symbol.Name;

    public IHasIndexers DeclaringType => resolvedDeclaringType ??= ResolveDeclaringType();

    IHasIndexers ResolveDeclaringType()
    {
        var containing = symbol.ContainingType;
        while (containing is not null && containing.TypeKind == TypeKind.Extension)
        {
            containing = containing.ContainingType;
        }

        return (containing is not null ? RectaleafRoslyn.FromSymbol(containing) as IHasIndexers : null)!;
    }

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IInheritanceKind<IIndexer<TKind, TAccess>> Inheritance => inheritance ??= ResolveInheritance();

    public TAccess Accessors => accessors;

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<IParameter> GetParameters()
        => parameters ??= symbol.Parameters.Select(p => (IParameter)new RoslynParameter(p)).ToList();

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();

    IInheritanceKind<IIndexer<TKind, TAccess>> ResolveInheritance()
    {
        if (symbol.IsAbstract)
            return RoslynInheritanceAbstract<IIndexer<TKind, TAccess>>.Instance;

        if (symbol.IsVirtual)
            return RoslynInheritanceVirtual<IIndexer<TKind, TAccess>>.Instance;

        if (symbol.IsOverride)
        {
            if (symbol.IsSealed)
                return new RoslynInheritanceSealedOverride<IIndexer<TKind, TAccess>>(() => (IIndexer<TKind, TAccess>)Create(symbol.OverriddenProperty!));
            return new RoslynInheritanceOverride<IIndexer<TKind, TAccess>>(() => (IIndexer<TKind, TAccess>)Create(symbol.OverriddenProperty!));
        }

        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return new RoslynInheritanceExplicitImplementation<IIndexer<TKind, TAccess>>(() => (IIndexer<TKind, TAccess>)Create(symbol.ExplicitInterfaceImplementations[0]));

        return RoslynInheritanceNone<IIndexer<TKind, TAccess>>.Instance;
    }

    public static IIndexer<IKnownOrNot, IAccess> Create(IPropertySymbol symbol, IHasIndexers? declaringType = null)
    {
        var typeUsage = RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation);
        bool isIncomplete = typeUsage is ITypeUsage<IIncomplete>;

        return isIncomplete
            ? CreateTyped<IIncomplete>(symbol, declaringType)
            : CreateTyped<IKnown>(symbol, declaringType);
    }

    static IIndexer<TTargetKind, IAccess> CreateTyped<TTargetKind>(IPropertySymbol symbol, IHasIndexers? declaringType)
        where TTargetKind : IKnownOrNot
    {
        if (symbol.ReturnsByRef || symbol.ReturnsByRefReadonly)
        {
            var elemType = (IComposableType<TTargetKind>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation);

            if (symbol.ReturnsByRefReadonly)
            {
                var byRef = new ByRefType<TTargetKind, IReadOnly>(elemType);
                return (IIndexer<TTargetKind, IAccess>)new RoslynRefIndexer<TTargetKind, IReadOnly>(symbol, byRef, declaringType);
            }
            else
            {
                var byRef = new ByRefType<TTargetKind, IMut>(elemType);
                return (IIndexer<TTargetKind, IAccess>)new RoslynRefIndexer<TTargetKind, IMut>(symbol, byRef, declaringType);
            }
        }

        var (getAccess, setAccess, isInit) = (symbol.GetMethod, symbol.SetMethod, symbol.SetMethod?.IsInitOnly == true);

        if (getAccess is not null && setAccess is not null)
        {
            if (isInit)
            {
                return (IIndexer<TTargetKind, IAccess>)new RoslynRegularIndexer<TTargetKind, IGetInit>(symbol, new RoslynIndexerGetInitAccessors(
                    new RoslynIndexerGetAccessor<IKnownOrNot>(getAccess, null!),
                    new RoslynIndexerInitAccessor<IKnownOrNot>(setAccess, null!)), declaringType);
            }

            return (IIndexer<TTargetKind, IAccess>)new RoslynRegularIndexer<TTargetKind, IGetSet>(symbol, new RoslynIndexerGetSetAccessors(
                new RoslynIndexerGetAccessor<IKnownOrNot>(getAccess, null!),
                new RoslynIndexerSetAccessor<IKnownOrNot>(setAccess, null!)), declaringType);
        }

        if (getAccess is not null)
        {
            return (IIndexer<TTargetKind, IAccess>)new RoslynRegularIndexer<TTargetKind, IGet>(symbol, new RoslynIndexerGetOnlyAccessors(
                new RoslynIndexerGetAccessor<IKnownOrNot>(getAccess, null!)), declaringType);
        }

        if (isInit)
        {
            return (IIndexer<TTargetKind, IAccess>)new RoslynRegularIndexer<TTargetKind, IInit>(symbol, new RoslynIndexerInitOnlyAccessors(
                new RoslynIndexerInitAccessor<IKnownOrNot>(setAccess!, null!)), declaringType);
        }

        return (IIndexer<TTargetKind, IAccess>)new RoslynRegularIndexer<TTargetKind, ISet>(symbol, new RoslynIndexerSetOnlyAccessors(
            new RoslynIndexerSetAccessor<IKnownOrNot>(setAccess!, null!)), declaringType);
    }
}

internal sealed class RoslynRegularIndexer<TKind, TAccess>(
    IPropertySymbol symbol,
    TAccess accessors,
    IHasIndexers? declaringType = null)
    : RoslynIndexer<TKind, TAccess>(symbol, accessors, declaringType), IRegularIndexer<TKind, TAccess>
    where TKind : IKnownOrNot
    where TAccess : IAccess
{
    IComposableType<TKind>? type;

    public IComposableType<TKind> Type
        => type ??= (IComposableType<TKind>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation);
}

internal sealed class RoslynRefIndexer<TKind, TMutability>(
    IPropertySymbol symbol,
    IByRefType<TKind, TMutability> byRefType,
    IHasIndexers? declaringType = null)
    : RoslynIndexer<TKind, IGet>(symbol, new RoslynIndexerGetOnlyAccessors(new RoslynIndexerGetAccessor<IKnownOrNot>(symbol.GetMethod!, null!)), declaringType),
      IRefIndexer<TKind, TMutability>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    public IByRefType<TKind, TMutability> Type => byRefType;
}
