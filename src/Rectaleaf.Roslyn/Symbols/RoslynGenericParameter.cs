using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Symbols;

internal sealed class RoslynGenericParameter(
    ITypeParameterSymbol symbol,
    Func<GenericParameterSource>? sourceResolver = null) : IGenericParameter
{
    GenericParameterSource? source;
    IReadOnlyList<ConstraintType>? constraintTypes;

    public string Name => symbol.Name;

    public int Position => symbol.Ordinal;

    public bool HasReferenceTypeConstraint
        => symbol.HasReferenceTypeConstraint && symbol.ReferenceTypeConstraintNullableAnnotation != NullableAnnotation.Annotated;

    public bool HasNullableReferenceTypeConstraint
        => symbol.HasReferenceTypeConstraint && symbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated;

    public bool HasNotNullConstraint => symbol.HasNotNullConstraint;

    public bool HasValueTypeConstraint
        => symbol.HasValueTypeConstraint && !symbol.HasUnmanagedTypeConstraint;

    public bool HasUnmanagedTypeConstraint => symbol.HasUnmanagedTypeConstraint;

    public bool HasConstructorConstraint => symbol.HasConstructorConstraint;

    public bool HasAllowsRefStructConstraint => symbol.AllowsRefLikeType;

    public bool HasDefaultConstraint => CheckDefaultConstraint(symbol);

    public GenericVariance Variance => symbol.Variance switch
    {
        VarianceKind.Out => GenericVariance.Covariant,
        VarianceKind.In => GenericVariance.Contravariant,
        _ => GenericVariance.Invariant
    };

    public IReadOnlyList<ConstraintType> ConstraintTypes
        => constraintTypes ??= symbol.ConstraintTypes
            .Select(ct =>
            {
                var usage = RoslynTypeUsageFactory.Create(ct, ct.NullableAnnotation);
                return usage is IPlaceholderType placeholder
                    ? (ConstraintType)new ConstraintPlaceholderType(placeholder)
                    : new ConstraintDeclaredType((IDeclTypeUsage<IKnownOrNot, IValidConstraint>)usage);
            })
            .ToList();

    public GenericParameterSource Source => source ??= (sourceResolver?.Invoke() ?? ResolveDefaultSource(symbol));

    public override bool Equals(object? obj)
        => obj is IGenericParameter other && GenericParameterEqualityComparer.Default.Equals(this, other);

    public override int GetHashCode()
        => GenericParameterEqualityComparer.Default.GetHashCode(this);

    static bool CheckDefaultConstraint(ITypeParameterSymbol typeParam)
    {
        foreach (var synRef in typeParam.DeclaringSyntaxReferences)
        {
            var node = synRef.GetSyntax();
            var parent = node.Parent;
            while (parent is not null && parent is not MemberDeclarationSyntax and not TypeDeclarationSyntax)
            {
                if (parent is TypeParameterConstraintClauseSyntax clause)
                {
                    if (clause.Name.Identifier.Text == typeParam.Name)
                    {
                        foreach (var c in clause.Constraints)
                        {
                            if (c is DefaultConstraintSyntax)
                                return true;
                        }
                    }
                }
                parent = parent.Parent;
            }
        }

        return false;
    }

    static GenericParameterSource ResolveDefaultSource(ITypeParameterSymbol typeParam)
    {
        var containing = typeParam.ContainingSymbol;

        if (containing is INamedTypeSymbol nts)
        {
            if (nts.TypeKind == TypeKind.Extension)
            {
                var declaringStatic = nts.ContainingType is not null
                    ? RectaleafRoslyn.FromSymbol(nts.ContainingType) as IStaticClassType
                    : null;
                return new GenericExtensionBlock(Roslyn.Members.RoslynExtensionsBlock.Create(nts, declaringStatic!));
            }

            return new GenericType(RectaleafRoslyn.FromSymbol(nts));
        }

        if (containing is IMethodSymbol ms)
            return new GenericMethod(RectaleafRoslyn.FromMethod(ms));

        throw new InvalidOperationException($"Unsupported generic parameter containing symbol: {containing}");
    }
}
