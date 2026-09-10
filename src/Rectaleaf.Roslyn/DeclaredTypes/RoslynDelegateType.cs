using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.DeclaredTypes;

internal sealed class RoslynDelegateType : RoslynDeclaredType, IDelegateType
{
    readonly RoslynTypeMembers members;
    IReturnInfo<IKnownOrNot>? returnInfo;
    IReadOnlyList<IParameter>? parameters;

    public RoslynDelegateType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public IReturnInfo<IKnownOrNot> ReturnInfo
        => returnInfo ??= new RoslynReturnInfo<IKnownOrNot>(symbol.DelegateInvokeMethod!);

    public IReadOnlyList<IParameter> GetParameters()
        => parameters ??= symbol.DelegateInvokeMethod is not null
            ? symbol.DelegateInvokeMethod.Parameters.Select(p => (IParameter)new RoslynParameter(p)).ToList()
            : Array.Empty<IParameter>();

    public IReadOnlyList<IGenericParameter> GetGenericParameters() => members.GetGenericParameters();

    public IReadOnlyList<IDeclaredType> GetNestedTypes() => members.GetNestedTypes();
}
