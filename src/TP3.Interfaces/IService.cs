using TP3.Interfaces;

public interface IService
{
    object State { get; }
    object Control { get; }
    object Events { get; }
    MetaData MetaData { get; }

    Task Init(IAgent me);
}