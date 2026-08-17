using Reticleave.Metadata.Members.PropertyFamily;
using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;

namespace Reticleave.Metadata.Members.PropertyFamily.Events;

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