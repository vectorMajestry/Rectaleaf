using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.Members.PropertyFamily;

/// <summary>
/// Represents any set of accessors of a property (any valid combination of get, set, and init)
/// </summary>
public interface IAccess;

/// <summary>
/// Represents a property that has a setter or an init accessor. 
/// </summary>
/// <remarks>
/// Property metadata objects should never be instantiated with this marker directly.
/// Therefore, it can only emerge as a result of upcasting of <see cref="ISet"/> or <see cref="IInit"/> 
/// via covariant generic parameters on interfaces derived from <see cref="IProperty{TKind, TAccess}"/>.
/// </remarks>
public interface IWritable : IAccess;

/// <summary>
/// Represents a property that has both a getter and a set/init accessor. 
/// </summary>
/// <remarks>
/// Property metadata objects should never be instantiated with this marker directly.
/// Therefore, it can only emerge as a result of upcasting of <see cref="IGetSet"/> or <see cref="IGetInit"/> 
/// via covariant generic parameters on interfaces derived from <see cref="IProperty{TKind, TAccess}"/>.
/// </remarks>
public interface IGetWritable : IGet, IWritable;

/// <summary>
/// Represents a property that has a getter.
/// </summary>
public interface IGet : IAccess
{
    /// <remarks>
    /// If the type of the property is classified as <see cref="IKnown"/>, returns 
    /// <see cref="IAccessor{TKind}"/> where TKind is <see cref="IKnown"/>, otherwise TKind is <see cref="IIncomplete"/>
    /// </remarks>
    IAccessor<IKnownOrNot> Getter { get; }
}

/// <summary>
/// Represents a property that has a setter. A subset of <see cref="IWritable"/>.
/// </summary>
public interface ISet : IWritable
{
    /// <remarks>
    /// If the type of the property is classified as <see cref="IKnown"/>, returns 
    /// <see cref="IAccessor{TKind}"/> where TKind is <see cref="IKnown"/>, otherwise TKind is <see cref="IIncomplete"/>
    /// </remarks>
    IAccessor<IKnownOrNot> Setter { get; }
}

/// <summary>
/// Represents a property that has an init accessor. A subset of <see cref="IWritable"/>.
/// </summary>
public interface IInit : IWritable
{
    /// <remarks>
    /// If the type of the property is classified as <see cref="IKnown"/>, returns 
    /// <see cref="IAccessor{TKind}"/> where TKind is <see cref="IKnown"/>, otherwise TKind is <see cref="IIncomplete"/>
    /// </remarks>
    IAccessor<IKnownOrNot> InitAccessor { get; }
}

/// <summary>
/// Represents a property that has both a getter and a setter.
/// </summary>
public interface IGetSet : IGet, ISet, IGetWritable;

/// <summary>
/// Represents a property that has both a getter and an init accessor.
/// </summary>
public interface IGetInit : IGet, IInit, IGetWritable;
