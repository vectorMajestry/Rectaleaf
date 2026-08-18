namespace Rectaleaf.Metadata.DeclaredTypes;

/// <summary>
/// Represents an instantiable class definition (non-abstract, non-static).
/// </summary>
public interface IRegularClassType : IClassType
{
    bool IsSealed { get; }
}
