using TP3.Interfaces;

namespace TP3.Agent.Logic.Transport;

public class UserSessions
{
    public UserSessions(string transportTag)
    {
        this.transportTag = transportTag;
    }

    private Dictionary<string, Connection> Connections = new();
    private string transportTag;

    public void AddSession(INetworkPipe session)
    {
        if(session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }
        if(string.IsNullOrEmpty(session.AgentID))
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
        return $"{transportTag}:{session.AgentID}";
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
        }else
        {
            throw new InvalidOperationException($"No connection found for session with AgentID: {session.AgentID}");
        }
    }

    internal INode FindNode(INetworkPipe incomingTransport, string tag)
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

    public Pointer GetPointer(INetworkPipe incomingTransport, string tag)
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
