using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.Remote;

internal class ControlNode : INode
{
    // this could exectue on the agent
    // maybe it could  get code and execute locally?
    
    public string Id => throw new NotImplementedException();

    public string Name => throw new NotImplementedException();

    public NodeType NodeType => throw new NotImplementedException();

    public IEnumerable<INode>? Children => throw new NotImplementedException();

    public Task<ITP3DataStream?> Get()
    {
        throw new NotImplementedException();
    }
}


public class BaseNode : INode
{
    
}