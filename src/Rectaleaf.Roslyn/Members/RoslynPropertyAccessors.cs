using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynPropertyAccessor<TKind>(
    IMethodSymbol symbol,
    IProperty<TKind, IAccess>? declaringProperty = null) : IPropertyAccessor<TKind>
    where TKind : IKnownOrNot
{
    protected readonly IMethodSymbol symbol = symbol;
    IReturnInfo<TKind>? returnInfo;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<ILocalFunction>? localFunctions;
    IProperty<TKind, IAccess>? resolvedDeclaringProperty = declaringProperty;

    public IProperty<TKind, IAccess> DeclaringProperty => resolvedDeclaringProperty ??= ResolveDeclaringProperty();

    IProperty<TKind, IAccess> ResolveDeclaringProperty()
    {
        if (symbol.AssociatedSymbol is IPropertySymbol propSym)
        {
            return (RectaleafRoslyn.FromProperty(propSym) as IProperty<TKind, IAccess>)!;
        }
        throw new InvalidOperationException($"Could not resolve declaring property for accessor {symbol.Name}");
    }

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IReturnInfo<TKind> ReturnInfo => returnInfo ??= new RoslynReturnInfo<TKind>(symbol);

    public bool IsReadOnly => symbol.IsReadOnly;

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<ILocalFunction> GetLocalFunctions()
        => localFunctions ??= RoslynLocalFunctionHelper.ExtractLocalFunctions(symbol, this);

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynPropertyGetAccessor<TKind>(
    IMethodSymbol symbol,
    IProperty<TKind, IAccess>? declaringProperty = null)
    : RoslynPropertyAccessor<TKind>(symbol, declaringProperty), IPropertyGetAccessor<TKind>
    where TKind : IKnownOrNot;

internal sealed class RoslynPropertySetAccessor<TKind>(
    IMethodSymbol symbol,
    IProperty<TKind, IAccess>? declaringProperty = null)
    : RoslynPropertyAccessor<TKind>(symbol, declaringProperty), IPropertySetAccessor<TKind>
    where TKind : IKnownOrNot
{
    IParameter? valueParameter;

    public IParameter ValueParameter
        => valueParameter ??= new RoslynParameter(symbol.Parameters.Last());
}

internal sealed class RoslynPropertyInitAccessor<TKind>(
    IMethodSymbol symbol,
    IProperty<TKind, IAccess>? declaringProperty = null)
    : RoslynPropertyAccessor<TKind>(symbol, declaringProperty), IPropertyInitAccessor<TKind>
    where TKind : IKnownOrNot
{
    IParameter? valueParameter;

    public IParameter ValueParameter
        => valueParameter ??= new RoslynParameter(symbol.Parameters.Last());
}

internal sealed class RoslynPropertyGetSetAccessors(
    IPropertyGetAccessor<IKnownOrNot> getter,
    IPropertySetAccessor<IKnownOrNot> setter) : IGetSet
{
    public IAccessor<IKnownOrNot> Getter => getter;
    public IAccessor<IKnownOrNot> Setter => setter;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynPropertyGetInitAccessors(
    IPropertyGetAccessor<IKnownOrNot> getter,
    IPropertyInitAccessor<IKnownOrNot> initAccessor) : IGetInit
{
    public IAccessor<IKnownOrNot> Getter => getter;
    public IAccessor<IKnownOrNot> InitAccessor => initAccessor;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynPropertyGetOnlyAccessors(
    IPropertyGetAccessor<IKnownOrNot> getter) : IGet
{
    public IAccessor<IKnownOrNot> Getter => getter;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynPropertySetOnlyAccessors(
    IPropertySetAccessor<IKnownOrNot> setter) : ISet
{
    public IAccessor<IKnownOrNot> Setter => setter;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynPropertyInitOnlyAccessors(
    IPropertyInitAccessor<IKnownOrNot> initAccessor) : IInit
{
    public IAccessor<IKnownOrNot> InitAccessor => initAccessor;

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}
