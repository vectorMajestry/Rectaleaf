using Reticleave.Metadata.Members.PropertyFamily;
using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;
using System.Diagnostics.CodeAnalysis;

namespace Reticleave.Metadata.Members.PropertyFamily.Properties;

/// <summary>
/// Represents a property accessor.
/// </summary>
/// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IPropertyGetAccessor{TKind}"/>, <see cref="IPropertySetAccessor{TKind}"/>, 
/// or <see cref="IPropertyInitAccessor{TKind}"/> instead.
/// </remarks>
public interface IPropertyAccessor<out TKind> : IAccessor<TKind>
    where TKind : IKnownOrNot
{
    IProperty<TKind, IAccess> DeclaringProperty { get; }
    AccessModifier AccessModifier { get; }
}

/// <summary>
/// Represents a property accessor that acts as an assignment target.
/// </summary>
/// <remarks>
/// This interface should not be implemented directly. 
/// Implement <see cref="IPropertySetAccessor{TKind}"/> or <see cref="IPropertyInitAccessor{TKind}"/> instead.
/// </remarks>
public interface IPropertyWriteAccessor<out TKind> : IPropertyAccessor<TKind>
    where TKind : IKnownOrNot
{
    IParameter ValueParameter { get; }
}

public interface IPropertyGetAccessor<out TKind> : IPropertyAccessor<TKind>
    where TKind : IKnownOrNot;

public interface IPropertySetAccessor<out TKind> : IPropertyWriteAccessor<TKind>
    where TKind : IKnownOrNot;

public interface IPropertyInitAccessor<out TKind> : IPropertyWriteAccessor<TKind>
    where TKind : IKnownOrNot;
