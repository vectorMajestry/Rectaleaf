namespace Reticleave.Metadata.TypeUsages.Interfaces;

public interface IWrapperType<out TKind> : IComposableType<TKind>
    where TKind : IKnownOrNot
{
    public IComposableType<TKind> ElementType { get; }
}

public interface IPointerType<out TKind> : IWrapperType<TKind>
    where TKind : IKnownOrNot;

public interface IArrayType<out TKind, out TRankKind> : IWrapperType<TKind>, IHasNullability
    where TKind : IKnownOrNot
{
    public int Rank { get; }
}

