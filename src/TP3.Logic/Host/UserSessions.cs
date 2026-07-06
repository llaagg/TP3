using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Agent.Logic.Transport;


class SessionNode : INode
{
    private KeyValuePair<string, Connection> connection1;


    public SessionNode(KeyValuePair<string, Connection> connection1)
    {
        this.connection1 = connection1;
    }

    public NodeType NodeType => NodeType.File;

    public string Id => $"Session_{connection1.Value.Session.AgentID}";

    public string Name => connection1.Key;

    public IEnumerable<INode>? Children => null;

    public async Task<ITP3DataStream?> Get()
    {
        return new TP3Stream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Session: {connection1.Key}"))); 
    }
}
