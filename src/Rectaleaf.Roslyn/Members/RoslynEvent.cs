using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Events;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynEvent(IEventSymbol symbol, IHasEvents? declaringType = null) : IEvent<IKnownOrNot>
{
    protected readonly IEventSymbol symbol = symbol;
    IHasEvents? resolvedDeclaringType = declaringType;
    IDeclTypeUsage<IKnownOrNot, IDelegateType>? type;
    IInheritanceKind<IEvent<IKnownOrNot>>? inheritance;
    IReadOnlyList<IAttribute>? attributes;
    IEventAddAccessor<IKnownOrNot>? addAccessor;
    IEventRemoveAccessor<IKnownOrNot>? removeAccessor;

    public string Name => symbol.Name;

    public IHasEvents DeclaringType => resolvedDeclaringType ??= (RectaleafRoslyn.FromSymbol(symbol.ContainingType) as IHasEvents)!;

    public IDeclTypeUsage<IKnownOrNot, IDelegateType> Type => type ??= ResolveType();

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IInheritanceKind<IEvent<IKnownOrNot>> Inheritance => inheritance ??= ResolveInheritance();

    public IEventAddAccessor<IKnownOrNot> AddAccessor => addAccessor ??= new RoslynEventAddAccessor<IKnownOrNot>(symbol.AddMethod!, this);

    public IEventRemoveAccessor<IKnownOrNot> RemoveAccessor => removeAccessor ??= new RoslynEventRemoveAccessor<IKnownOrNot>(symbol.RemoveMethod!, this);

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();

    IDeclTypeUsage<IKnownOrNot, IDelegateType> ResolveType()
    {
        var usage = RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation);
        return (IDeclTypeUsage<IKnownOrNot, IDelegateType>)usage;
    }

    IInheritanceKind<IEvent<IKnownOrNot>> ResolveInheritance()
    {
        if (symbol.IsAbstract)
            return RoslynInheritanceAbstract<IEvent<IKnownOrNot>>.Instance;

        if (symbol.IsVirtual)
            return RoslynInheritanceVirtual<IEvent<IKnownOrNot>>.Instance;

        if (symbol.IsOverride)
        {
            if (symbol.IsSealed)
                return new RoslynInheritanceSealedOverride<IEvent<IKnownOrNot>>(() => Create(symbol.OverriddenEvent!));
            return new RoslynInheritanceOverride<IEvent<IKnownOrNot>>(() => Create(symbol.OverriddenEvent!));
        }

        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return new RoslynInheritanceExplicitImplementation<IEvent<IKnownOrNot>>(() => Create(symbol.ExplicitInterfaceImplementations[0]));

        return RoslynInheritanceNone<IEvent<IKnownOrNot>>.Instance;
    }

    public static IEvent<IKnownOrNot> Create(IEventSymbol symbol, IHasEvents? declaringType = null)
    {
        if (symbol.IsStatic)
            return new RoslynStaticEvent(symbol, declaringType);

        return new RoslynInstanceEvent(symbol, declaringType);
    }
}

internal sealed class RoslynStaticEvent(IEventSymbol symbol, IHasEvents? declaringType = null)
    : RoslynEvent(symbol, declaringType), IStaticEvent<IKnownOrNot>;

internal sealed class RoslynInstanceEvent(IEventSymbol symbol, IHasEvents? declaringType = null)
    : RoslynEvent(symbol, declaringType), IInstanceEvent<IKnownOrNot>;

internal abstract class RoslynEventAccessor<TKind>(
    IMethodSymbol symbol,
    IEvent<TKind> declaringEvent) : IEventAccessor<TKind>
    where TKind : IKnownOrNot
{
    protected readonly IMethodSymbol symbol = symbol;
    IReturnInfo<TKind>? returnInfo;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<ILocalFunction>? localFunctions;
    IParameter? valueParameter;

    public IEvent<TKind> DeclaringEvent => declaringEvent;

    public IParameter ValueParameter => valueParameter ??= new RoslynParameter(symbol.Parameters[0]);

    public IReturnInfo<TKind> ReturnInfo => returnInfo ??= new RoslynReturnInfo<TKind>(symbol);

    public bool IsReadOnly => symbol.IsReadOnly;

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<ILocalFunction> GetLocalFunctions()
        => localFunctions ??= RoslynLocalFunctionHelper.ExtractLocalFunctions(symbol, this);

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynEventAddAccessor<TKind>(
    IMethodSymbol symbol,
    IEvent<TKind> declaringEvent)
    : RoslynEventAccessor<TKind>(symbol, declaringEvent), IEventAddAccessor<TKind>
    where TKind : IKnownOrNot;

internal sealed class RoslynEventRemoveAccessor<TKind>(
    IMethodSymbol symbol,
    IEvent<TKind> declaringEvent)
    : RoslynEventAccessor<TKind>(symbol, declaringEvent), IEventRemoveAccessor<TKind>
    where TKind : IKnownOrNot;
