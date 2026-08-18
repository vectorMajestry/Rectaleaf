using Rectaleaf.Components;
using Rectaleaf.Members;
using Rectaleaf.Metadata.Members;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a static class definition.
/// </summary>
public interface IStaticClassType : IReferenceType, IHasFields, IHasMethods, IHasProperties,
    IHasEvents, IHasGenericParameters, IHasStaticConstructor, IHasNestedTypes, IHasOperators;
