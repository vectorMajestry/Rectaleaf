using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.Members.PropertyFamily.Events;

public interface IEvent<TKind>
    where TKind : IKnownOrNot
{
    string Name { get; }
    IHasEvents DeclaringType { get; }
    IDeclTypeUsage<TKind, IDelegateType> Type { get; }
    AccessModifier AccessModifier { get; }
    IInheritanceKind<IEvent<TKind>> Inheritance { get; }
    IReadOnlyList<IAttribute> GetAttributes();

    IEventAddAccessor<TKind> AddAccessor { get; }
    IEventRemoveAccessor<TKind> RemoveAccessor { get; }
}

public interface IStaticEvent<TKind> : IEvent<TKind>
    where TKind : IKnownOrNot;
public interface IInstanceEvent<TKind> : IEvent<TKind>
    where TKind : IKnownOrNot;
