using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynProperty<TKind, TAccess>(
    IPropertySymbol symbol,
    TAccess accessors,
    IHasProperties? declaringType = null) : IProperty<TKind, TAccess>
    where TKind : IKnownOrNot
    where TAccess : IAccess
{
    protected readonly IPropertySymbol symbol = symbol;
    IHasProperties? resolvedDeclaringType = declaringType;
    IInheritanceKind<IProperty<TKind, TAccess>>? inheritance;
    IReadOnlyList<IAttribute>? attributes;

    public string Name => symbol.Name;

    public IHasProperties DeclaringType => resolvedDeclaringType ??= ResolveDeclaringType();

    IHasProperties ResolveDeclaringType()
    {
        var containing = symbol.ContainingType;
        while (containing is not null && containing.TypeKind == TypeKind.Extension)
        {
            containing = containing.ContainingType;
        }

        return (containing is not null ? RectaleafRoslyn.FromSymbol(containing) as IHasProperties : null)!;
    }

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IInheritanceKind<IProperty<TKind, TAccess>> Inheritance => inheritance ??= ResolveInheritance();

    public TAccess Accessors => accessors;

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();

    IInheritanceKind<IProperty<TKind, TAccess>> ResolveInheritance()
    {
        if (symbol.IsAbstract)
            return RoslynInheritanceAbstract<IProperty<TKind, TAccess>>.Instance;

        if (symbol.IsVirtual)
            return RoslynInheritanceVirtual<IProperty<TKind, TAccess>>.Instance;

        if (symbol.IsOverride)
        {
            if (symbol.IsSealed)
                return new RoslynInheritanceSealedOverride<IProperty<TKind, TAccess>>(() => (IProperty<TKind, TAccess>)RectaleafRoslyn.FromProperty(symbol.OverriddenProperty!));
            return new RoslynInheritanceOverride<IProperty<TKind, TAccess>>(() => (IProperty<TKind, TAccess>)RectaleafRoslyn.FromProperty(symbol.OverriddenProperty!));
        }

        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return new RoslynInheritanceExplicitImplementation<IProperty<TKind, TAccess>>(() => (IProperty<TKind, TAccess>)RectaleafRoslyn.FromProperty(symbol.ExplicitInterfaceImplementations[0]));

        return RoslynInheritanceNone<IProperty<TKind, TAccess>>.Instance;
    }

    public static IProperty<IKnownOrNot, IAccess> Create(IPropertySymbol symbol, IHasProperties? declaringType = null)
    {
        var typeUsage = RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation);
        bool isIncomplete = typeUsage is ITypeUsage<IIncomplete>;

        return isIncomplete
            ? CreateTyped<IIncomplete>(symbol, declaringType)
            : CreateTyped<IKnown>(symbol, declaringType);
    }

    static IProperty<TTargetKind, IAccess> CreateTyped<TTargetKind>(IPropertySymbol symbol, IHasProperties? declaringType)
        where TTargetKind : IKnownOrNot
    {
        if (symbol.ReturnsByRef || symbol.ReturnsByRefReadonly)
        {
            var elemType = (IComposableType<TTargetKind>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation);

            if (symbol.ReturnsByRefReadonly)
            {
                var byRef = new ByRefType<TTargetKind, IReadOnly>(elemType);
                return symbol.IsStatic
                    ? (IProperty<TTargetKind, IAccess>)new RoslynStaticRefProperty<TTargetKind, IReadOnly>(symbol, byRef, declaringType)
                    : (IProperty<TTargetKind, IAccess>)new RoslynInstanceRefProperty<TTargetKind, IReadOnly>(symbol, byRef, declaringType);
            }
            else
            {
                var byRef = new ByRefType<TTargetKind, IMut>(elemType);
                return symbol.IsStatic
                    ? (IProperty<TTargetKind, IAccess>)new RoslynStaticRefProperty<TTargetKind, IMut>(symbol, byRef, declaringType)
                    : (IProperty<TTargetKind, IAccess>)new RoslynInstanceRefProperty<TTargetKind, IMut>(symbol, byRef, declaringType);
            }
        }

        var (getAccess, setAccess, isInit) = (symbol.GetMethod, symbol.SetMethod, symbol.SetMethod?.IsInitOnly == true);

        if (getAccess is not null && setAccess is not null)
        {
            if (isInit)
            {
                return symbol.IsStatic
                    ? (IProperty<TTargetKind, IAccess>)new RoslynStaticProperty<TTargetKind, IGetInit>(symbol, new RoslynPropertyGetInitAccessors(
                        new RoslynPropertyGetAccessor<IKnownOrNot>(getAccess, null!),
                        new RoslynPropertyInitAccessor<IKnownOrNot>(setAccess, null!)), declaringType)
                    : (IProperty<TTargetKind, IAccess>)new RoslynInstanceProperty<TTargetKind, IGetInit>(symbol, new RoslynPropertyGetInitAccessors(
                        new RoslynPropertyGetAccessor<IKnownOrNot>(getAccess, null!),
                        new RoslynPropertyInitAccessor<IKnownOrNot>(setAccess, null!)), declaringType);
            }

            return symbol.IsStatic
                ? (IProperty<TTargetKind, IAccess>)new RoslynStaticProperty<TTargetKind, IGetSet>(symbol, new RoslynPropertyGetSetAccessors(
                    new RoslynPropertyGetAccessor<IKnownOrNot>(getAccess, null!),
                    new RoslynPropertySetAccessor<IKnownOrNot>(setAccess, null!)), declaringType)
                : (IProperty<TTargetKind, IAccess>)new RoslynInstanceProperty<TTargetKind, IGetSet>(symbol, new RoslynPropertyGetSetAccessors(
                    new RoslynPropertyGetAccessor<IKnownOrNot>(getAccess, null!),
                    new RoslynPropertySetAccessor<IKnownOrNot>(setAccess, null!)), declaringType);
        }

        if (getAccess is not null)
        {
            return symbol.IsStatic
                ? (IProperty<TTargetKind, IAccess>)new RoslynStaticProperty<TTargetKind, IGet>(symbol, new RoslynPropertyGetOnlyAccessors(
                    new RoslynPropertyGetAccessor<IKnownOrNot>(getAccess, null!)), declaringType)
                : (IProperty<TTargetKind, IAccess>)new RoslynInstanceProperty<TTargetKind, IGet>(symbol, new RoslynPropertyGetOnlyAccessors(
                    new RoslynPropertyGetAccessor<IKnownOrNot>(getAccess, null!)), declaringType);
        }

        if (isInit)
        {
            return symbol.IsStatic
                ? (IProperty<TTargetKind, IAccess>)new RoslynStaticProperty<TTargetKind, IInit>(symbol, new RoslynPropertyInitOnlyAccessors(
                    new RoslynPropertyInitAccessor<IKnownOrNot>(setAccess!, null!)), declaringType)
                : (IProperty<TTargetKind, IAccess>)new RoslynInstanceProperty<TTargetKind, IInit>(symbol, new RoslynPropertyInitOnlyAccessors(
                    new RoslynPropertyInitAccessor<IKnownOrNot>(setAccess!, null!)), declaringType);
        }

        return symbol.IsStatic
            ? (IProperty<TTargetKind, IAccess>)new RoslynStaticProperty<TTargetKind, ISet>(symbol, new RoslynPropertySetOnlyAccessors(
                new RoslynPropertySetAccessor<IKnownOrNot>(setAccess!, null!)), declaringType)
            : (IProperty<TTargetKind, IAccess>)new RoslynInstanceProperty<TTargetKind, ISet>(symbol, new RoslynPropertySetOnlyAccessors(
                new RoslynPropertySetAccessor<IKnownOrNot>(setAccess!, null!)), declaringType);
    }
}

internal abstract class RoslynRegularProperty<TKind, TAccess>(
    IPropertySymbol symbol,
    TAccess accessors,
    IHasProperties? declaringType = null)
    : RoslynProperty<TKind, TAccess>(symbol, accessors, declaringType), IRegularProperty<TKind, TAccess>
    where TKind : IKnownOrNot
    where TAccess : IAccess
{
    IComposableType<TKind>? type;
    IRegularField<TKind>? backingField;
    bool backingFieldResolved;

    public IComposableType<TKind> Type
        => type ??= (IComposableType<TKind>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation);

    public IRegularField<TKind>? BackingField
    {
        get
        {
            if (!backingFieldResolved)
            {
                backingField = ResolveBackingField();
                backingFieldResolved = true;
            }
            return backingField;
        }
    }

    IRegularField<TKind>? ResolveBackingField()
    {
        if (symbol.ContainingType is null)
            return null;

        foreach (var member in symbol.ContainingType.GetMembers())
        {
            if (member is IFieldSymbol f && SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, symbol))
                return RoslynField<TKind>.Create(f, DeclaringType as IHasFields) as IRegularField<TKind>;
        }

        return null;
    }
}

internal sealed class RoslynStaticProperty<TKind, TAccess>(
    IPropertySymbol symbol,
    TAccess accessors,
    IHasProperties? declaringType = null)
    : RoslynRegularProperty<TKind, TAccess>(symbol, accessors, declaringType), IStaticProperty<TKind, TAccess>
    where TKind : IKnownOrNot
    where TAccess : IAccess;

internal sealed class RoslynInstanceProperty<TKind, TAccess>(
    IPropertySymbol symbol,
    TAccess accessors,
    IHasProperties? declaringType = null)
    : RoslynRegularProperty<TKind, TAccess>(symbol, accessors, declaringType), IInstanceProperty<TKind, TAccess>, IPropertyRequirement
    where TKind : IKnownOrNot
    where TAccess : IAccess
{
    public bool IsRequired => symbol.IsRequired;
}

internal sealed class RoslynStaticRefProperty<TKind, TMutability>(
    IPropertySymbol symbol,
    IByRefType<TKind, TMutability> byRefType,
    IHasProperties? declaringType = null)
    : RoslynProperty<TKind, IGet>(symbol, new RoslynPropertyGetOnlyAccessors(new RoslynPropertyGetAccessor<IKnownOrNot>(symbol.GetMethod!, null!)), declaringType),
      IStaticRefProperty<TKind, TMutability>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    public IByRefType<TKind, TMutability> Type => byRefType;
}

internal sealed class RoslynInstanceRefProperty<TKind, TMutability>(
    IPropertySymbol symbol,
    IByRefType<TKind, TMutability> byRefType,
    IHasProperties? declaringType = null)
    : RoslynProperty<TKind, IGet>(symbol, new RoslynPropertyGetOnlyAccessors(new RoslynPropertyGetAccessor<IKnownOrNot>(symbol.GetMethod!, null!)), declaringType),
      IInstanceRefProperty<TKind, TMutability>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    public IByRefType<TKind, TMutability> Type => byRefType;
}
