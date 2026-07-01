using TP3.Interfaces;

public interface IService
{
    public INode State { get; }
    public INode Control { get; }
    public INode Events { get; }

    bool CanHandlePath(IReadOnlyList<string> fullPath);

    Task Init(IAgent me);
}