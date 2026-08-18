using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Components;

public abstract record AttributeValue;

public abstract record AttributeValueUnit : AttributeValue;

public sealed record AttributeValueArray(IReadOnlyList<AttributeValueUnit> Items) 
    : AttributeValue;

public sealed record AttributeValuePrimitive(IPredefinedType Type, object? Value) 
    : AttributeValueUnit;

public sealed record AttributeValueType(ITypeUsage<IKnown>? TargetType) 
    : AttributeValueUnit;

public sealed record AttributeValueEnum(IEnumType EnumType, object? MemberValue) 
    : AttributeValueUnit;

