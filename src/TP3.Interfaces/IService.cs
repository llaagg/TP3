using TP3.Interfaces;

public interface IService : INode, IDisposable
{
    Task Init(IAgent me);

    Task Start();

    Task Stop();
}