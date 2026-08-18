using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;

namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents a declared type definition (e.g., class, struct, record, interface, enum, delegate).
/// </summary>
public interface IDeclaredType
{
    string FullyQualifiedName { get; }
    string LocalName { get; }
    string? Namespace { get; }
    IHasNestedTypes? ContainingType { get; }
    IReadOnlyList<IAttribute> GetAttributes();
    AccessModifier AccessModifier { get; }
}
