using TP3.Interfaces;

namespace TP3.Agent.Logic.Transport;

public class UserSessions
{
    private Dictionary<string, INetworkPipe> sessions = new();

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

        sessions[session.AgentID] = session;
    }
}