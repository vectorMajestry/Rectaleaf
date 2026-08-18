using Rectaleaf.Components;

namespace Rectaleaf.Metadata.Members;

public interface IAttribute
{
    string Name { get; }
    string FullyQualifiedName { get; }

    IReadOnlyList<AttributeValue> ConstructorArguments { get; }
    IReadOnlyDictionary<string, AttributeValue> NamedArguments { get; }
}

