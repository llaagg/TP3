using TP3.Interfaces;

public interface IService
{
    public INode State { get; }
    public INode Control { get; }
    public INode Events { get; }
    Task Init(IAgent me);
    Task<ITP3DataStream> Open(INode node);
}