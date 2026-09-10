using System;
using System.Collections.Generic;
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
                    ? RectaleafRoslyn.FromSymbol(symbol.ContainingType) as IHasNestedTypes
                    : null;
                containingTypeResolved = true;
            }
            return containingType;
        }
    }

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

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
