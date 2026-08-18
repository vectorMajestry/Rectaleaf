using Rectaleaf.Components;
using Rectaleaf.Members;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents an enum definition.
/// </summary>
public interface IEnumType : IValueType, IHasSize, IHasNestedTypes, IValidConstraint
{
    IReadOnlyList<IEnumMember> Members { get; }
    IEnumUnderlyingType GetUnderlyingType();
}

/// <summary>
/// Represents an enum definition with a known underlying integer representation.
/// </summary>
/// <typeparam name="T">
/// The underlying type of the enum members. 
/// Corresponds to one of '<see cref="EnumUnderlyingTypeEnum"/>' members.
/// </typeparam>
public interface IEnumType<T> : IEnumType
    where T : struct
{
    new IReadOnlyList<IEnumMember<T>> Members { get; }
}
