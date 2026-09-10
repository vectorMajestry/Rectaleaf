using Rectaleaf.Components;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a delegate definition.
/// </summary>
public interface IDelegateType : IReferenceType, IHasGenericParameters, IHasNestedTypes
{
    IReturnInfo<IKnownOrNot> ReturnInfo { get; }
    IReadOnlyList<IParameter> GetParameters();
}