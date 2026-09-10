using System;
using System.Collections.Generic;
using System.Linq;
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

internal sealed class RoslynStructType : RoslynDeclaredType, IStructType
{
    readonly RoslynTypeMembers members;

    public RoslynStructType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public bool IsReadOnly => symbol.IsReadOnly;

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
}

internal sealed class RoslynRefStructType : RoslynDeclaredType, IRefStructType
{
    readonly RoslynTypeMembers members;

    public RoslynRefStructType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public bool IsReadOnly => symbol.IsReadOnly;

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
}

internal sealed class RoslynRecordStructType : RoslynDeclaredType, IRecordStructType
{
    readonly RoslynTypeMembers members;

    public RoslynRecordStructType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public bool IsReadOnly => symbol.IsReadOnly;

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
}

internal sealed class RoslynInlineArrayType : RoslynDeclaredType, IInlineArrayType
{
    readonly RoslynTypeMembers members;
    IComposableType<IKnownOrNot>? elementType;
    int? length;

    public RoslynInlineArrayType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public bool IsReadOnly => symbol.IsReadOnly;

    public int Length => length ??= ResolveLength();

    public IComposableType<IKnownOrNot> ElementType => elementType ??= ResolveElementType();

    public IReadOnlyList<IMethod> GetMethods() => members.GetMethods();
    public IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties() => members.GetProperties();
    public IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers() => members.GetIndexers();
    public IReadOnlyList<IEvent<IKnownOrNot>> GetEvents() => members.GetEvents();
    public IReadOnlyList<IInstanceConstructor> GetInstanceConstructors() => members.GetInstanceConstructors();
    public IStaticConstructor? StaticConstructor => members.GetStaticConstructor();
    public IReadOnlyList<IDeclaredType> GetNestedTypes() => members.GetNestedTypes();
    public IReadOnlyList<IDeclTypeUsage<IKnownOrNot, IInterfaceType>> GetDirectInterfaces() => members.GetDirectInterfaces();
    public IReadOnlyList<IGenericParameter> GetGenericParameters() => members.GetGenericParameters();
    public IReadOnlyList<IOperator> GetOperators() => members.GetOperators();

    int ResolveLength()
    {
        var attr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "InlineArrayAttribute");
        if (attr is not null && attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int len)
            return len;

        return 0;
    }

    IComposableType<IKnownOrNot> ResolveElementType()
    {
        var firstField = symbol.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(f => !f.IsStatic);
        if (firstField is not null)
            return (IComposableType<IKnownOrNot>)RoslynTypeUsageFactory.Create(firstField.Type, firstField.NullableAnnotation);

        throw new InvalidOperationException($"Inline array struct '{symbol.Name}' must have an instance backing field.");
    }
}
