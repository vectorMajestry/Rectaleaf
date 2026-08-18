using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Components;

namespace Rectaleaf.Metadata.Members.PropertyFamily.Events;

public interface IEventAccessor<TKind> : IAccessor<TKind>
    where TKind : IKnownOrNot
{
    IEvent<TKind> DeclaringEvent { get; }
    IParameter ValueParameter { get; }
}

public interface IEventAddAccessor<TKind> : IAccessor<TKind>
    where TKind : IKnownOrNot;

    public interface IEventRemoveAccessor<TKind> : IAccessor<TKind>
    where TKind : IKnownOrNot;