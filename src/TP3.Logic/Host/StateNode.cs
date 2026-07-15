using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public class StateNode : BaseDirectoryNode
{
    public StateNode(INetworkSessions userSessions)
        : base("state")
    {
        this.connectionsNode = new ConnectionsNode(userSessions);
    }

    private INode connectionsNode;


    public override IEnumerable<INode>? Children => new List<INode>() { connectionsNode };

}

public class ConnectionsNode : INode
{
    private INetworkSessions userSessions;

    public ConnectionsNode(INetworkSessions userSessions)
    {
        this.userSessions = userSessions;
    }

    public string Id => "ConnectionsNode";

    public string Name => "Connections";

    public NodeType NodeType => NodeType.Directory;

    public IEnumerable<INode>? Children => GetChildern();

    public ulong Length => 0;
    private IEnumerable<INode>? GetChildern()
    {
        var userSessions = this.userSessions as NetworkSessions;
        foreach (var connection in userSessions!.Connections)
        {
            var sessionNode = new SessionNode(connection);
            yield return sessionNode;
        }
    }

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
}