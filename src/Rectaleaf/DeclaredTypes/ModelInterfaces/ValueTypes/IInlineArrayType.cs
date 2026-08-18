using Rectaleaf.Components;
using Rectaleaf.Members;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents an inline array struct definition (decorated with <see cref="System.Runtime.CompilerServices.InlineArrayAttribute"/>).
/// </summary>
public interface IInlineArrayType : IValueType, IHasMethods, IHasProperties, IHasIndexers,
    IHasEvents, IHasGenericParameters, IHasInstanceConstructors, IHasStaticConstructor,
    IHasNestedTypes, IHasInterfaces, IHasOperators, IValidConstraint, IHasSize
{
    /// <summary>
    /// Gets the element type of this inline array.
    /// </summary>
    IComposableType<IKnownOrNot> ElementType { get; }

    /// <summary>
    /// Gets the fixed length of the inline array.
    /// </summary>
    int Length { get; }

    bool IsReadOnly { get; }
}
