using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class ExplicitValueTupleType<TKind>(
    IBoundGenericType<TKind, IStructType> underlyingValueTupleType,
    NullabilityKind nullability) : TupleType<TKind>(underlyingValueTupleType, nullability), IExplicitValueTupleType<TKind>
    where TKind : IKnownOrNot;
