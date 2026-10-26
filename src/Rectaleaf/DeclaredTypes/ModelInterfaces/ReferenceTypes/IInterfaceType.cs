using Rectaleaf.Components;
using Rectaleaf.Metadata.Members;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents an interface definition.
/// </summary>
public interface IInterfaceType : IReferenceType, IHasMethods, IHasProperties, IHasIndexers, IHasEvents,
    IHasGenericParameters, IHasStaticConstructor, IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint;
