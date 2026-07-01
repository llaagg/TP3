namespace TP3.Agent.Logic.Agent;

internal class ServiceNode : INode
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

    public Stream? Data => null;
}

internal class StateNode : INode
{
    private INode state;

    public StateNode(INode state)
    {
        this.state = state;

    }

    public string Name => "state";

    public IEnumerable<INode>? Children => state.Children;

    public Stream? Data => null;
}