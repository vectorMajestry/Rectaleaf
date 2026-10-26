using Rectaleaf.Components;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.Members;

public interface IAttribute
{
    IDeclTypeUsage<IKnownOrNot, IRegularClassType> UnderlyingType { get; }
    IInstanceConstructor UsedConstructor { get; }
    IReadOnlyList<AttributeValue> ConstructorArguments { get; }
    IReadOnlyDictionary<string, AttributeValue> NamedArguments { get; }
}