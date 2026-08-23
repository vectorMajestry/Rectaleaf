using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;


namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class BoundGenericType<TKind, TDecl>(TDecl genericType, IReadOnlyList<IComposableType<IKnownOrNot>> typeArguments, NullabilityKind nullability)
    : TypeUsage<TKind>, IBoundGenericType<TKind, TDecl>
    where TKind : IKnownOrNot
    where TDecl : IDeclaredType, IHasGenericParameters
{
    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(GetNotNullName())
            : GetNotNullName();
            
    public TDecl TypeDefinition { get; } = genericType;
    public NullabilityKind Nullability { get; } = nullability;
    public IReadOnlyList<IComposableType<IKnownOrNot>> TypeArgumentsMixed { get; } = typeArguments;
    IDeclaredType IDeclTypeUsage<TKind>.TypeDefinition => TypeDefinition;

    string GetNotNullName() => WrapperNameFormatter.GenericName(TypeDefinition.LocalName, TypeArgumentsMixed.Select(t => t.LocalName));
}