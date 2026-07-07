using TP3.Interfaces;

public interface IService : INode
{
    Task Init(IAgent me);
}