using Rectaleaf.Components;
using Rectaleaf.Members;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a class definition (instantiable or abstract).
/// </summary>
public interface IClassType : IReferenceType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    IDeclTypeUsage<IKnownOrNot, IClassType>? BaseType { get; }
}
