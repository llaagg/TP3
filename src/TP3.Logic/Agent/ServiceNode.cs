using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Agent;

internal partial class ServiceNode : INode
{
    private readonly IService s;

    public ServiceNode(IService s)
    {
        this.s = s;
    }

    public string Id => $"service:{Name}";

    public string Name => s.GetType().Name;

    public NodeType NodeType => NodeType.Directory;

    public IEnumerable<INode>? Children
    {
        get
        {
            if (s.State != null) 
            {
                yield return new StateNode(s.State);
                
            }
            // if (s.Control != null) children.Add(s.Control);
            // if (s.Events != null) children.Add(s.Events);   
        }
    }

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
}