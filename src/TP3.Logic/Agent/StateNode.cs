using TP3.Interfaces;

namespace TP3.Agent.Logic.Agent;

public class StateNode : INode
{
    private INode state;

    public StateNode(INode state)
    {
        this.state = state;

    }

    public string Qid => $"state:{Name}";

    public string Name => "state";

    public IEnumerable<INode>? Children => state.Children;

    public ITP3Stream? Data => null;
}
