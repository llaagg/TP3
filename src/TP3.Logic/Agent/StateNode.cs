using TP3.Interfaces;
using TP3.Messages;

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

    public NodeType NodeType => NodeType.Directory;

    public IEnumerable<INode>? Children => state.Children;

    public async Task<ITP3DataStream?> Get()
    {
        return null!;
    }
}
