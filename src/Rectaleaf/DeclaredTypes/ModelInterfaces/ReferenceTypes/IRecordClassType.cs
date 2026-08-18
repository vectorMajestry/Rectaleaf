using Rectaleaf.Components;
using Rectaleaf.Members;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a record class definition.
/// </summary>
public interface IRecordClassType : IReferenceType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    IDeclTypeUsage<IKnownOrNot, IRecordClassType>? BaseType { get; }
    bool IsSealed { get; }
}
