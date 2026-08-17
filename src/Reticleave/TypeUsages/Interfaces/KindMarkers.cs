namespace Reticleave.Metadata.TypeUsages.Interfaces;

public interface IKnownOrNot;
public interface IKnown : IKnownOrNot;
public interface IIncomplete : IKnownOrNot;

public interface IMutOrNot;
public interface IMut : IMutOrNot;
public interface IReadOnly : IMutOrNot;

public interface IFlatOrNot;
public interface IFlat : IFlatOrNot;
public interface IRectArr : IFlatOrNot;

