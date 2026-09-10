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
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.DeclaredTypes;

internal abstract class RoslynClassType : RoslynDeclaredType, IClassType
{
    protected readonly RoslynTypeMembers members;
    IDeclTypeUsage<IKnownOrNot, IClassType>? baseType;
    bool baseTypeResolved;

    public RoslynClassType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public IDeclTypeUsage<IKnownOrNot, IClassType>? BaseType
    {
        get
        {
            if (!baseTypeResolved)
            {
                baseType = ResolveBaseType();
                baseTypeResolved = true;
            }
            return baseType;
        }
    }

    public IReadOnlyList<IMethod> GetMethods() => members.GetMethods();
    public IReadOnlyList<IField<IKnownOrNot>> GetFields() => members.GetFields();
    public IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties() => members.GetProperties();
    public IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers() => members.GetIndexers();
    public IReadOnlyList<IEvent<IKnownOrNot>> GetEvents() => members.GetEvents();
    public IReadOnlyList<IInstanceConstructor> GetInstanceConstructors() => members.GetInstanceConstructors();
    public IStaticConstructor? StaticConstructor => members.GetStaticConstructor();
    public IReadOnlyList<IDeclaredType> GetNestedTypes() => members.GetNestedTypes();
    public IReadOnlyList<IDeclTypeUsage<IKnownOrNot, IInterfaceType>> GetDirectInterfaces() => members.GetDirectInterfaces();
    public IReadOnlyList<IGenericParameter> GetGenericParameters() => members.GetGenericParameters();
    public IReadOnlyList<IOperator> GetOperators() => members.GetOperators();

    IDeclTypeUsage<IKnownOrNot, IClassType>? ResolveBaseType()
    {
        if (symbol.BaseType is null || symbol.BaseType.SpecialType == SpecialType.System_Object)
            return null;

        var usage = RoslynTypeUsageFactory.Create(symbol.BaseType, NullableAnnotation.NotAnnotated);
        return usage as IDeclTypeUsage<IKnownOrNot, IClassType>;
    }
}

internal sealed class RoslynRegularClassType : RoslynClassType, IRegularClassType
{
    public RoslynRegularClassType(INamedTypeSymbol symbol) : base(symbol)
    {
    }

    public bool IsSealed => symbol.IsSealed;
}

internal sealed class RoslynAbstractClassType : RoslynClassType, IAbstractClassType
{
    public RoslynAbstractClassType(INamedTypeSymbol symbol) : base(symbol)
    {
    }
}

internal sealed class RoslynRecordClassType : RoslynDeclaredType, IRecordClassType
{
    readonly RoslynTypeMembers members;
    IDeclTypeUsage<IKnownOrNot, IRecordClassType>? baseType;
    bool baseTypeResolved;

    public RoslynRecordClassType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public bool IsSealed => symbol.IsSealed;

    public IDeclTypeUsage<IKnownOrNot, IRecordClassType>? BaseType
    {
        get
        {
            if (!baseTypeResolved)
            {
                baseType = ResolveBaseType();
                baseTypeResolved = true;
            }
            return baseType;
        }
    }

    public IReadOnlyList<IMethod> GetMethods() => members.GetMethods();
    public IReadOnlyList<IField<IKnownOrNot>> GetFields() => members.GetFields();
    public IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties() => members.GetProperties();
    public IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers() => members.GetIndexers();
    public IReadOnlyList<IEvent<IKnownOrNot>> GetEvents() => members.GetEvents();
    public IReadOnlyList<IInstanceConstructor> GetInstanceConstructors() => members.GetInstanceConstructors();
    public IStaticConstructor? StaticConstructor => members.GetStaticConstructor();
    public IReadOnlyList<IDeclaredType> GetNestedTypes() => members.GetNestedTypes();
    public IReadOnlyList<IDeclTypeUsage<IKnownOrNot, IInterfaceType>> GetDirectInterfaces() => members.GetDirectInterfaces();
    public IReadOnlyList<IGenericParameter> GetGenericParameters() => members.GetGenericParameters();
    public IReadOnlyList<IOperator> GetOperators() => members.GetOperators();

    IDeclTypeUsage<IKnownOrNot, IRecordClassType>? ResolveBaseType()
    {
        if (symbol.BaseType is null || symbol.BaseType.SpecialType == SpecialType.System_Object || !symbol.BaseType.IsRecord)
            return null;

        var usage = RoslynTypeUsageFactory.Create(symbol.BaseType, NullableAnnotation.NotAnnotated);
        return usage as IDeclTypeUsage<IKnownOrNot, IRecordClassType>;
    }
}

internal sealed class RoslynStaticClassType : RoslynDeclaredType, IStaticClassType
{
    readonly RoslynTypeMembers members;

    public RoslynStaticClassType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public IReadOnlyList<IMethod> GetMethods() => members.GetMethods();
    public IReadOnlyList<IField<IKnownOrNot>> GetFields() => members.GetFields();
    public IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties() => members.GetProperties();
    public IReadOnlyList<IEvent<IKnownOrNot>> GetEvents() => members.GetEvents();
    public IStaticConstructor? StaticConstructor => members.GetStaticConstructor();
    public IReadOnlyList<IDeclaredType> GetNestedTypes() => members.GetNestedTypes();
    public IReadOnlyList<IGenericParameter> GetGenericParameters() => members.GetGenericParameters();
    public IReadOnlyList<IOperator> GetOperators() => members.GetOperators();
    public IReadOnlyList<IExtensionsBlock> GetExtensionsBlocks() => members.GetExtensionsBlocks();
}
