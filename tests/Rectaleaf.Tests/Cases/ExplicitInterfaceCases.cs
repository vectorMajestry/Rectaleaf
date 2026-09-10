namespace TestCases;

using System;
using System.Runtime.CompilerServices;

public interface IWorker
{
    string WorkerName { get; }
    void PerformWork(int taskId);
    int this[int priority] { get; }
    event Action? WorkCompleted;
}

public interface ICustomInterfaceIndexer
{
    [IndexerName("CustomInterfaceIndexer")]
    string this[int index] { get; }
}

public interface ISecondaryIndexed
{
    [IndexerName("SecondaryEntries")]
    double this[string key] { get; set; }
}

public class ExplicitWorkerImplementation : IWorker, ICustomInterfaceIndexer, IDisposable
{
    public string NormalProperty => "Ordinary";

    // Explicit interface method
    void IDisposable.Dispose()
    {
    }

    // Explicit interface property
    string IWorker.WorkerName => "ExplicitWorker";

    // Explicit interface method
    void IWorker.PerformWork(int taskId)
    {
    }

    // Explicit interface indexer (default named on IWorker)
    int IWorker.this[int priority] => priority * 10;

    // Explicit interface event
    event Action? IWorker.WorkCompleted
    {
        add { }
        remove { }
    }

    // Class's own indexer with custom name attribute
    [IndexerName("ClassIndexer")]
    public int this[int index] => index * 2;

    // Explicit implementation of interface with custom IndexerName attribute
    string ICustomInterfaceIndexer.this[int index] => $"item_{index}";
}

// Base class and derived class testing member hiding with 'new'
public class BaseWorker
{
    public int StateField = 1;

    public virtual string Title { get; set; } = "BaseTitle";

    public void DoAction()
    {
    }
}

public class DerivedHidingWorker : BaseWorker
{
    // Member hiding with new on field
    public new int StateField = 2;

    // Member hiding with new on property
    public new string Title { get; set; } = "DerivedTitle";

    // Member hiding with new on method
    public new void DoAction()
    {
    }
}

// Diamond interface hierarchy
public interface IRootService
{
    void Initialize();
}

public interface IDataService : IRootService
{
    void LoadData();
}

public interface ILoggingService : IRootService
{
    void FlushLogs();
}

public class DiamondServiceImplementation : IDataService, ILoggingService
{
    public void Initialize()
    {
    }

    public void LoadData()
    {
    }

    public void FlushLogs()
    {
    }
}
