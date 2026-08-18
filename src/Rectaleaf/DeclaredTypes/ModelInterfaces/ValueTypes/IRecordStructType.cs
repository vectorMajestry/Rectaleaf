using Rectaleaf.Components;
using Rectaleaf.Members;
using Rectaleaf.Metadata.Members;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a record struct definition.
/// </summary>
public interface IRecordStructType : IValueType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint
{
    bool IsReadOnly { get; }
}
