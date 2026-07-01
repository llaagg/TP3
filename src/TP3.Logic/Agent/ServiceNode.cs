using TP3.Interfaces;

namespace TP3.Agent.Logic.Agent;

internal partial class ServiceNode : INode
{
    private readonly IService s;

    public ServiceNode(IService s)
    {
        this.s = s;
    }

    public string Name => s.GetType().Name;

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

    ITP3Stream? INode.Data => null;

}