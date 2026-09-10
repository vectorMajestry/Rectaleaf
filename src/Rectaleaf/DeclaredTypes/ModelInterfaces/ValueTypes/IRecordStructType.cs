namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a record struct definition.
/// </summary>
public interface IRecordStructType : IValueType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators
{
    bool IsReadOnly { get; }
}
