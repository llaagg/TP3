using TP3.Interfaces;

public interface IService : INode, IDisposable
{
    /// <summary>
    /// Optional service metadata subtree (for example: /services/{service}/meta).
    /// </summary>
    INode? MetaNode => null;

    Task Init(IAgent me);

    Task Start();

    Task Stop();
}