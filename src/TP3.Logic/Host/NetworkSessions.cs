using TP3.Interfaces;

namespace TP3.Agent.Logic.Transport;

public class NetworkSessions : INetworkSessions
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

    /// <summary>
    /// Attaches a tag to user session, this can happen only once per tag per session. If the tag is already attached to a session, it will be overwritten.
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="node"></param>
    /// <param name="session"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void AttachTagToPointer(string tag, INode node, INetworkPipe session)
    {
        var connectionId = GetConnectionId(session);

        if(string.IsNullOrEmpty(tag))
        {
            throw new ArgumentException("Tag cannot be null or empty.", nameof(tag));
        }
        
        if (Connections.TryGetValue(connectionId, out var connection))
        {
            if(connection.Pointers.ContainsKey(tag))
            {
                throw new InvalidOperationException($"Tag '{tag}' is already attached to this session.");
            }

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

    public void CloseSession(INetworkPipe incomingTransport, string tag)
    {
        var connectionId = GetConnectionId(incomingTransport);
        if (Connections.TryGetValue(connectionId, out var connection))
        {
            if (connection.Pointers.ContainsKey(tag))
            {
                connection.Pointers.Remove(tag);
            }
        }else
        {
            throw new InvalidOperationException($"No connection found for session with AgentID: {incomingTransport.AgentID}");
        }
    }
}
