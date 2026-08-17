namespace Reticleave.Metadata.TypeUsages.Interfaces;

public interface ITypeUsage<out TKind>
    where TKind : IKnownOrNot
{
    public string LocalName { get; }
}

