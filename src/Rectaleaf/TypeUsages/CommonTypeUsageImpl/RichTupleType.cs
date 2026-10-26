using Rectaleaf.Helpers;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class RichTupleType<TKind> : TypeUsage<TKind>, IRichTupleType<TKind>
    where TKind : IKnownOrNot
{
    public RichTupleType(
        IReadOnlyList<ITupleElement<TKind>> elements,
        IBoundGenericType<TKind, IStructType> underlyingValueTupleType,
        NullabilityKind nullability)
    {
        Elements = elements;
        UnderlyingValueTupleType = underlyingValueTupleType;
        Nullability = nullability;
    }

    public override string LocalName =>
        field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(GetNotNullName())
            : GetNotNullName();

    public IReadOnlyList<ITupleElement<TKind>> Elements { get; }

    public IBoundGenericType<TKind, IStructType> UnderlyingValueTupleType { get; }

    public NullabilityKind Nullability { get; }

#pragma warning disable CS0618
    public IReadOnlyList<IComposableType<IKnownOrNot>> ElementTypesMixed
        => field ??= Elements.Select(e => (IComposableType<IKnownOrNot>)e.Type).ToArray();
#pragma warning restore CS0618

    string GetNotNullName() =>
        $"({string.Join(", ", Elements.Select(e => e.Name is not null ? $"{e.Type.LocalName} {e.Name}" : e.Type.LocalName))})";
}
