using TP3.Interfaces;

namespace TP3.Service.Remote;

public class RemoteNodes : BaseDirectoryNode
{
    private readonly List<INode> connections = new();

    public RemoteNodes() : base("state")
    {
    }

    public void AddConnection(INode serverNode)
    {
        this.connections.Add(serverNode);
    }

    override public IEnumerable<INode>? Children => this.connections;
}