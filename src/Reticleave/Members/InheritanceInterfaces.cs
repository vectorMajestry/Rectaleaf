namespace Reticleave.Metadata.Members;

/// <summary>
/// The base contract for describing the inheritance modifiers and dispatch semantics of a declaration.
/// </summary>
public interface IInheritanceKind<out TMember>;

/// <summary>
/// Specifies that a declaration is non-virtual, statically bound, and carries no inheritance modifiers 
/// (such as virtual, abstract, or override) at its declaration site. May implicitly implement an 
/// interface member.
/// </summary>
public interface IInheritanceNone<out TMember> : IInheritanceKind<TMember>;

/// <summary>
/// Specifies that a declaration introduces a virtual member with no default implementation, 
/// requiring non-abstract derived types to provide an override.
/// </summary>
public interface IInheritanceAbstract<out TMember> : IInheritanceKind<TMember>;

/// <summary>
/// Specifies that a declaration introduces a virtual member along with a default implementation, 
/// allowing derived types to provide an override.
/// </summary>
public interface IInheritanceVirtual<out TMember> : IInheritanceKind<TMember>;

/// <summary>
/// Specifies that a declaration provides a new implementation for an inherited virtual or abstract slot.
/// </summary>
public interface IInheritanceOverride<out TMember> : IInheritanceKind<TMember>
{
    /// <summary>
    /// Gets the immediate member being overridden (not the root declaration).
    /// </summary>
    TMember OverriddenMember { get; }
}

/// <summary>
/// Specifies that an override is marked with the sealed modifier, preventing further overriding in derived classes.
/// </summary>
public interface IInheritanceSealedOverride<out TMember> : IInheritanceOverride<TMember>;

/// <summary>
/// Specifies an explicit interface implementation. The member is only accessible on the target interface type.
/// </summary>
public interface IInheritanceExplicitImplementation<out TMember> : IInheritanceKind<TMember>
{
    /// <summary>
    /// Gets the interface member being explicitly implemented.
    /// </summary>
    TMember ExplicitInterfaceMember { get; }
}
