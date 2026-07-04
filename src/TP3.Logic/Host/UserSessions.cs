using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public class UserSessions : INetworkSessions
{
    public Dictionary<string, Connection> Connections = new();

    public void AddSession(INetworkPipe session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }
        if (string.IsNullOrEmpty(session.AgentID))
        {
            throw new ArgumentException("Session must have a valid AgentID.", nameof(session));
        }

        Connections[GetConnectionId(session)] = new Connection
        {
            Session = session
        };
    }

    string GetConnectionId(INetworkPipe session)
    {
        return $"{session.TP3Transport.TransportTag}:{session.AgentID}";
    }
    public void AttachTagToPointer(string tag, INode node, INetworkPipe session)
    {
        var connectionId = GetConnectionId(session);

        if (Connections.TryGetValue(connectionId, out var connection))
        {
            connection.Pointers[tag] = new Pointer
            {
                Node = node
            };
        }
        else
        {
            throw new InvalidOperationException($"No connection found for session with AgentID: {session.AgentID}");
        }
    }

    public INode FindNode(INetworkPipe incomingTransport, string tag)
    {
        var connectionId = GetConnectionId(incomingTransport);
        if (Connections.TryGetValue(connectionId, out var connection))
        {
            if (connection.Pointers.TryGetValue(tag, out var node))
            {
                return node.Node;
            }
        }
        return null!;
    }

    public IPointer GetPointer(INetworkPipe incomingTransport, string tag)
    {
        var connectionId = GetConnectionId(incomingTransport);
        if (Connections.TryGetValue(connectionId, out var connection))
        {
            if (connection.Pointers.TryGetValue(tag, out var pointer))
            {
                return pointer;
            }
        }
        return null!;
    }


}


class SessionNode : INode
{
    private KeyValuePair<string, Connection> connection1;


    public SessionNode(KeyValuePair<string, Connection> connection1)
    {
        this.connection1 = connection1;
    }

    public NodeType NodeType => NodeType.Directory;

    public string Id => $"Session_{connection1.Value.Session.AgentID}";

    public string Name => connection1.Key;

    public IEnumerable<INode>? Children => null;

    public async Task<ITP3DataStream?> Get()
    {
        return new TP3Stream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Session: {connection1.Key}"))); 
    }
}


class StateNode : INode
{
    private INetworkSessions userSessions;

    public StateNode(INetworkSessions userSessions)
    {
        this.userSessions = userSessions;
    }

    public NodeType NodeType => NodeType.Directory;

    public string Id => "UserSessionsStateNode";

    public IEnumerable<INode>? Children
    {
        get
        {
            foreach (var connection in (userSessions as UserSessions).Connections)
            {
                yield return new SessionNode(connection);
            }
        }
    }

    public string Name => throw new NotImplementedException();

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
}
