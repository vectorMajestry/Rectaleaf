using Rectaleaf.Components;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.Members.PropertyFamily.Events;

public interface IEventAccessor<out TKind> : IAccessor<TKind>
    where TKind : IKnownOrNot
{
    IEvent<TKind> DeclaringEvent { get; }
    IParameter ValueParameter { get; }
}

public interface IEventAddAccessor<out TKind> : IAccessor<TKind>
    where TKind : IKnownOrNot;

public interface IEventRemoveAccessor<out TKind> : IAccessor<TKind>
where TKind : IKnownOrNot;