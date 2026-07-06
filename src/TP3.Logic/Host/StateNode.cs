using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public class StateNode : INode
{
    public StateNode(INetworkSessions userSessions)
    {
        this.connectionsNode = new ConnectionsNode(userSessions);
    }

    private INode connectionsNode;

    public NodeType NodeType => NodeType.Directory;

    public string Id => "UserSessionsStateNode";

    public IEnumerable<INode>? Children
    {
        get
        {
            yield return connectionsNode;
        }
    }

    public string Name => "Connections";

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
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