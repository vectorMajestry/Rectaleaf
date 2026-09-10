using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Events;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Roslyn.DeclaredTypes;

internal sealed class RoslynInterfaceType : RoslynDeclaredType, IInterfaceType
{
    readonly RoslynTypeMembers members;

    public RoslynInterfaceType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public IReadOnlyList<IMethod> GetMethods() => members.GetMethods();
    public IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties() => members.GetProperties();
    public IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers() => members.GetIndexers();
    public IReadOnlyList<IEvent<IKnownOrNot>> GetEvents() => members.GetEvents();
    public IStaticConstructor? StaticConstructor => members.GetStaticConstructor();
    public IReadOnlyList<IDeclaredType> GetNestedTypes() => members.GetNestedTypes();
    public IReadOnlyList<IDeclTypeUsage<IKnownOrNot, IInterfaceType>> GetDirectInterfaces() => members.GetDirectInterfaces();
    public IReadOnlyList<IGenericParameter> GetGenericParameters() => members.GetGenericParameters();
    public IReadOnlyList<IOperator> GetOperators() => members.GetOperators();
}
