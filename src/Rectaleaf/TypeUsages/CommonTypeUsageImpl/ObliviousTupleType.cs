using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class ObliviousTupleType<TKind>(
    IBoundGenericType<TKind, IStructType> underlyingValueTupleType,
    NullabilityKind nullability) : TupleType<TKind>(underlyingValueTupleType, nullability), IObliviousTupleType<TKind>
    where TKind : IKnownOrNot;