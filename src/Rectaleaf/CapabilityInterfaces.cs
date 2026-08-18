using Rectaleaf.Components;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Events;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata;

public interface IHasMethods : IDeclaredType
{
    IReadOnlyList<IMethod> GetMethods();
}

public interface IHasSize : IDeclaredType
{
    int SizeInBytes { get; }
}

public interface IHasFields : IDeclaredType, IHasSize
{
    IReadOnlyList<IField<IKnownOrNot>> GetFields();
}

public interface IHasProperties : IDeclaredType
{
    IReadOnlyList<IProperty<IKnownOrNot, IAccess>> GetProperties();
}

public interface IHasIndexers : IDeclaredType
{
    IReadOnlyList<IIndexer<IKnownOrNot, IAccess>> GetIndexers();
}

public interface IHasEvents : IDeclaredType
{
    IReadOnlyList<IEvent<IKnownOrNot>> GetEvents();
}

public interface IHasInstanceConstructors : IDeclaredType
{
    IReadOnlyList<IInstanceConstructor> GetInstanceConstructors();
}

public interface IHasStaticConstructor : IDeclaredType
{
    IStaticConstructor? StaticConstructor { get; }
}

public interface IHasGenericParameters : IDeclaredType
{
    IReadOnlyList<IGenericParameter> GetGenericParameters();
}

public interface IHasLocalFunctions
{
    IReadOnlyList<ILocalFunction> GetLocalFunctions();
}

public interface IHasNestedTypes : IDeclaredType
{
    IReadOnlyList<IDeclaredType> GetNestedTypes();
}

public interface IValidConstraint;

public interface IHasInterfaces : IDeclaredType
{
    /// <summary>
    /// Gets the interfaces directly implemented by this declared type. This excludes interfaces
    /// implemented by the base type, as well as any inherited (transitive) interfaces of the directly 
    /// implemented interfaces.
    /// </summary>
    IReadOnlyList<IDeclTypeUsage<IKnownOrNot, IInterfaceType>> GetDirectInterfaces();
}

public interface IHasNullability
{
    NullabilityKind Nullability { get; }
}

public interface IHasOperators : IDeclaredType
{
    IReadOnlyList<IOperator> GetOperators();
}
