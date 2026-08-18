using Rectaleaf.Components;
using Rectaleaf.Members;
using Rectaleaf.Metadata.Members;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a standard struct definition. Not a ref struct.
/// </summary>
public interface IStructType : IValueType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    bool IsReadOnly { get; }
}
