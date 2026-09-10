using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynMethod(IMethodSymbol symbol, IHasMethods? declaringType = null) : IMethod
{
    protected readonly IMethodSymbol symbol = symbol;
    IHasMethods? resolvedDeclaringType = declaringType;
    IReturnInfo<IKnownOrNot>? returnInfo;
    IInheritanceKind<IMethod>? inheritance;
    IReadOnlyList<IParameter>? parameters;
    IReadOnlyList<IGenericParameter>? genericParameters;
    IReadOnlyList<IAttribute>? attributes;
    IReadOnlyList<ILocalFunction>? localFunctions;

    public string Name => symbol.Name;

    public IReturnInfo<IKnownOrNot> ReturnInfo => returnInfo ??= new RoslynReturnInfo<IKnownOrNot>(symbol);

    public IHasMethods DeclaringType => resolvedDeclaringType ??= ResolveDeclaringType();

    IHasMethods ResolveDeclaringType()
    {
        var containing = symbol.ContainingType;
        while (containing is not null && containing.TypeKind == TypeKind.Extension)
        {
            containing = containing.ContainingType;
        }

        return (containing is not null ? RectaleafRoslyn.FromSymbol(containing) as IHasMethods : null)!;
    }

    public bool IsGeneric => symbol.IsGenericMethod;

    public bool IsReadOnly => symbol.IsReadOnly;

    public bool HasArglistParameter => symbol.IsVararg;

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IInheritanceKind<IMethod> Inheritance => inheritance ??= ResolveInheritance();

    public IReadOnlyList<IParameter> GetParameters()
        => parameters ??= symbol.Parameters.Select(p => (IParameter)new RoslynParameter(p)).ToList();

    public IReadOnlyList<IGenericParameter> GetGenericParameters()
        => genericParameters ??= symbol.TypeParameters
            .Select(tp => (IGenericParameter)new RoslynGenericParameter(tp, () => new GenericMethod(this)))
            .ToList();

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public IReadOnlyList<ILocalFunction> GetLocalFunctions()
        => localFunctions ??= RoslynLocalFunctionHelper.ExtractLocalFunctions(symbol, this);

    public override bool Equals(object? obj)
        => obj is IMethod other && MethodEqualityComparer.Default.Equals(this, other);

    public override int GetHashCode()
        => MethodEqualityComparer.Default.GetHashCode(this);

    IInheritanceKind<IMethod> ResolveInheritance()
    {
        if (symbol.IsAbstract)
            return RoslynInheritanceAbstract<IMethod>.Instance;

        if (symbol.IsVirtual)
            return RoslynInheritanceVirtual<IMethod>.Instance;

        if (symbol.IsOverride)
        {
            if (symbol.IsSealed)
                return new RoslynInheritanceSealedOverride<IMethod>(() => RectaleafRoslyn.FromMethod(symbol.OverriddenMethod!));
            return new RoslynInheritanceOverride<IMethod>(() => RectaleafRoslyn.FromMethod(symbol.OverriddenMethod!));
        }

        if (symbol.ExplicitInterfaceImplementations.Length > 0)
            return new RoslynInheritanceExplicitImplementation<IMethod>(() => RectaleafRoslyn.FromMethod(symbol.ExplicitInterfaceImplementations[0]));

        return RoslynInheritanceNone<IMethod>.Instance;
    }
}

internal sealed class RoslynStaticMethod(IMethodSymbol symbol, IHasMethods? declaringType = null)
    : RoslynMethod(symbol, declaringType), IStaticMethod;

internal sealed class RoslynInstanceMethod(IMethodSymbol symbol, IHasMethods? declaringType = null)
    : RoslynMethod(symbol, declaringType), IInstMethod;
