using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Events;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Members;
using Rectaleaf.Roslyn.Symbols;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.DeclaredTypes;

internal sealed class RoslynTypeMembers(INamedTypeSymbol symbol, IDeclaredType declaringType)
{
    IReadOnlyList<IMethod>? methods;
    IReadOnlyList<IField<IKnownOrNot>>? fields;
    IReadOnlyList<IProperty<IKnownOrNot, IAccess>>? properties;
    IReadOnlyList<IIndexer<IKnownOrNot, IAccess>>? indexers;
    IReadOnlyList<IEvent<IKnownOrNot>>? events;
    IReadOnlyList<IInstanceConstructor>? instanceConstructors;
    IStaticConstructor? staticConstructor;
    bool staticConstructorResolved;
    IReadOnlyList<IDeclaredType>? nestedTypes;
    IReadOnlyList<IDeclTypeUsage<IKnownOrNot, IInterfaceType>>? directInterfaces;
    IReadOnlyList<IGenericParameter>? genericParameters;
    IReadOnlyList<IOperator>? operators;
    IReadOnlyList<IExtensionsBlock>? extensionsBlocks;

    public IReadOnlyList<IMethod> GetMethods()
        => methods ??= symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m =>
                (m.MethodKind is MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation) &&
                !m.Name.StartsWith("<") &&
                !IsRecordSynthesizedBoilerplate(m) &&
                !IsExtensionBlockMethod(m))
            .Select(m => m.IsStatic ? (IMethod)new RoslynStaticMethod(m, declaringType as IHasMethods) : new RoslynInstanceMethod(m, declaringType as IHasMethods))
            .ToList();

    public IReadOnlyList<IField<IKnownOrNot>> GetFields()
        => fields ??= symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f =>
                f.AssociatedSymbol is null &&
                !f.IsImplicitlyDeclared &&
                !f.Name.Contains("<"))
            .Select(f => RoslynField<IKnownOrNot>.Create(f, declaringType as IHasFields))
            .ToList();

    public IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties()
        => properties ??= symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p =>
                !p.IsIndexer &&
                !(p.ContainingType.IsRecord && p.Name == "EqualityContract" && p.IsImplicitlyDeclared))
            .Select(p => RoslynProperty<IKnownOrNot, IAccess>.Create(p, declaringType as IHasProperties))
            .ToList();

    public IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers()
        => indexers ??= symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsIndexer)
            .Select(p => RoslynIndexer<IKnownOrNot, IAccess>.Create(p, declaringType as IHasIndexers))
            .ToList();

    public IReadOnlyList<IEvent<IKnownOrNot>> GetEvents()
        => events ??= symbol.GetMembers()
            .OfType<IEventSymbol>()
            .Select(e => RoslynEvent.Create(e, declaringType as IHasEvents))
            .ToList();

    public IReadOnlyList<IInstanceConstructor> GetInstanceConstructors()
        => instanceConstructors ??= symbol.InstanceConstructors
            .Where(c => !c.ContainingType.IsAnonymousType)
            .Select(c => (IInstanceConstructor)new RoslynInstanceConstructor(c, declaringType as IHasInstanceConstructors))
            .ToList();

    public IStaticConstructor? GetStaticConstructor()
    {
        if (!staticConstructorResolved)
        {
            staticConstructor = symbol.StaticConstructors.FirstOrDefault() is { } sc
                ? new RoslynStaticConstructor(sc, declaringType as IHasStaticConstructor)
                : null;
            staticConstructorResolved = true;
        }
        return staticConstructor;
    }

    public IReadOnlyList<IDeclaredType> GetNestedTypes()
        => nestedTypes ??= symbol.GetTypeMembers()
            .Where(tm => tm.TypeKind != TypeKind.Extension && !tm.Name.StartsWith("<") && !tm.IsImplicitlyDeclared)
            .Select(tm => RectaleafRoslyn.FromSymbol(tm))
            .ToList();

    public IReadOnlyList<IDeclTypeUsage<IKnownOrNot, IInterfaceType>> GetDirectInterfaces()
        => directInterfaces ??= symbol.Interfaces
            .Select(iface => (IDeclTypeUsage<IKnownOrNot, IInterfaceType>)RoslynTypeUsageFactory.Create(iface, iface.NullableAnnotation))
            .ToList();

    public IReadOnlyList<IGenericParameter> GetGenericParameters()
        => genericParameters ??= symbol.TypeParameters
            .Select(tp => (IGenericParameter)new RoslynGenericParameter(tp, () => new GenericType(declaringType)))
            .ToList();

    public IReadOnlyList<IOperator> GetOperators()
        => operators ??= symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion)
            .Select(m => RoslynOperator.Create(m)!)
            .Where(op => op is not null)
            .ToList();

    public IReadOnlyList<IExtensionsBlock> GetExtensionsBlocks()
        => extensionsBlocks ??= symbol.GetTypeMembers()
            .Where(tm => tm.TypeKind == TypeKind.Extension)
            .Select(tm => RoslynExtensionsBlock.Create(tm, (IStaticClassType)declaringType))
            .ToList();

    static bool IsRecordSynthesizedBoilerplate(IMethodSymbol m)
    {
        if (!m.ContainingType.IsRecord)
            return false;

        if (!m.IsImplicitlyDeclared)
            return false;

        if (m.Name is "PrintMembers" or "Deconstruct")
            return false;

        if (m.Name is "Equals" or "GetHashCode" or "ToString" or "<Clone>$")
            return true;

        return false;
    }

    static bool IsExtensionBlockMethod(IMethodSymbol m)
    {
        foreach (var synRef in m.DeclaringSyntaxReferences)
        {
            if (synRef.GetSyntax().Ancestors().OfType<ExtensionBlockDeclarationSyntax>().Any())
                return true;
        }
        return false;
    }
}

