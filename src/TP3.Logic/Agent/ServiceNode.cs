namespace TP3.Agent.Logic.Agent;

internal class ServiceNode : INode
{
    private IService s;

    public ServiceNode(IService s)
    {
        this.s = s;
    }

    public string Name => s.GetType().Name;

    public IEnumerable<INode>? Children
    {
        get
        {
            var children = new List<INode>();
            if (s.State != null) children.Add(s.State);
            if (s.Control != null) children.Add(s.Control);
            if (s.Events != null) children.Add(s.Events);   
            return children;
        }
    }

    public Stream? Data => null;
}