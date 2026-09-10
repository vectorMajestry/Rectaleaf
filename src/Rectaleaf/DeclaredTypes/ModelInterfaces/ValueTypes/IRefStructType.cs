namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a ref struct definition.
/// </summary>
public interface IRefStructType : IValueType, IHasFields, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators
{
    bool IsReadOnly { get; }
}
