using Rectaleaf.Metadata.Members;

namespace Rectaleaf.Members;

public interface IEnumMember
{
    string Name { get; }
    object Value { get; }
    IReadOnlyList<IAttribute> GetAttributes();
}

public interface IEnumMember<T> : IEnumMember
    where T : struct
{
    new T Value { get; }
}

