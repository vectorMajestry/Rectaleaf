using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.DeclaredTypes.Comparers;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.DeclaredTypes;

internal abstract class RoslynDeclaredType(INamedTypeSymbol symbol) : IDeclaredType
{
    protected readonly INamedTypeSymbol symbol = symbol;
    IReadOnlyList<IAttribute>? attributes;
    IHasNestedTypes? containingType;
    bool containingTypeResolved;

    public string FullyQualifiedName => symbol.ToDisplayString();

    public string LocalName => CleanLocalName(symbol.Name);

    public string? Namespace => symbol.ContainingNamespace is null || symbol.ContainingNamespace.IsGlobalNamespace
        ? null
        : symbol.ContainingNamespace.ToDisplayString();

    public IHasNestedTypes? ContainingType
    {
        get
        {
            if (!containingTypeResolved)
            {
                containingType = symbol.ContainingType is not null
                    ? RectaleafRoslyn.FromTypeDefinition(symbol.ContainingType) as IHasNestedTypes
                    : null;
                containingTypeResolved = true;
            }
            return containingType;
        }
    }

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= ResolveAttributes();

    IReadOnlyList<IAttribute> ResolveAttributes()
    {
        ImmutableArray<AttributeData> rawAttrs = symbol.GetAttributes();
        if (rawAttrs.IsDefaultOrEmpty)
            return Array.Empty<IAttribute>();

        bool hasDefaultMemberAttr = false;
        for (int i = 0; i < rawAttrs.Length; i++)
        {
            if (rawAttrs[i].AttributeClass?.Name == "DefaultMemberAttribute")
            {
                hasDefaultMemberAttr = true;
                break;
            }
        }

        bool excludeDefaultMember = false;
        if (hasDefaultMemberAttr)
        {
            foreach (ISymbol member in symbol.GetMembers())
            {
                if (member is IPropertySymbol { IsIndexer: true })
                {
                    excludeDefaultMember = true;
                    break;
                }
            }
        }

        return RoslynAttribute.FilterAndCreate(rawAttrs, excludeDefaultMember);
    }

    public override bool Equals(object? obj)
        => obj is IDeclaredType other && DeclaredTypeEqualityComparer.Default.Equals(this, other);

    public override int GetHashCode()
        => DeclaredTypeEqualityComparer.Default.GetHashCode(this);

    static string CleanLocalName(string name)
    {
        int idx = name.IndexOf("__", StringComparison.Ordinal);
        if (idx >= 0 && name.StartsWith("<", StringComparison.Ordinal))
            return name.Substring(idx + 2);
        return name;
    }
}
