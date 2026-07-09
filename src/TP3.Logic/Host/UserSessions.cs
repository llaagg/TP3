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

    public NodeType NodeType => NodeType.Directory;

    public string Id => $"Session_{connection1.Value.Session.AgentID}";

    public string Name => connection1.Key+"("+getNetworkDetails()+")";

    public string getNetworkDetails()
    {
        var session = connection1.Value.Session;
        if (session is INetworkPipe ipcSession)
        {
            return $"{ipcSession.ToString()}";
        }else
        {
            return "";
        }
    }

    public IEnumerable<INode>? Children
    {
        get
        {
            return this.connection1.Value.Pointers.Select(kvp => new PointerNode(kvp.Key, kvp.Value));
        }
    }

    public async Task<ITP3DataStream?> Get()
    {
        return null;
    }
}

internal class PointerNode : INode
{
    private IPointer pointer;
    private string name;

    public PointerNode(string name, IPointer pointer)
    {
        this.pointer = pointer;
        this.name = name;
    }

    public string Id => name;

    public string Name => $"pointer_{name}";

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => null;

    public Task<ITP3DataStream?> Get()
    {
        return Task.FromResult<ITP3DataStream?>(new TP3Stream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"Pointer: {name} Data: {pointer.Node.Name}({pointer.Node.NodeType} {pointer.Node.Id}) Positions: {pointer.Data?.Position}"))));
    }
}