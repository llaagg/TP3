using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

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
            var userSessions = this.userSessions as UserSessions;
            foreach (var connection in userSessions!.Connections)
            {
                var sessionNode = new SessionNode(connection);
                yield return sessionNode;
            }
        }
    }

    public string Name => "Connections";

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(null);
    }
}
