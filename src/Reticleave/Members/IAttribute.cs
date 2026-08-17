using Reticleave.Components;

namespace Reticleave.Metadata.Members;

public interface IAttribute
{
    string Name { get; }
    string FullyQualifiedName { get; }

    IReadOnlyList<AttributeValue> ConstructorArguments { get; }
    IReadOnlyDictionary<string, AttributeValue> NamedArguments { get; }
}

