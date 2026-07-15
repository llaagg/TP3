using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Agent;

/// <summary>
/// this is the place where all streams will be availble
/// covering:
///  storage 
///  events (incl. events that allow to start compute) like bus of events
///  stream (incl. multimedia streams)
/// each stream should provide metadata, 
///  r or w what is there, where is it, what is the transport
///  is 
///  
/// </summary>
public class Trunk : INode
{
    private IList<IService> services;

    public Trunk(IList<IService> services)
    {
        this.services = services;
    }

    public string Id => "/";

    public string Name => "/";

    public NodeType NodeType => NodeType.Directory;

    public IEnumerable<INode>? Children
    {
        get
        {
            yield return new BaseDirectoryNode("services", "services", services.Cast<INode>().ToList());
        }
    }

    public ulong Length => throw new NotImplementedException();

    public async Task<ITP3DataStream?> Get()
    {
        return null;
    }
}
