using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.Remote;

public class RemotesService : BaseDirectoryNode, IService
{
    public RemotesService() : base()
    {
        this.State = new RemoteNodes();
    }

    public async Task Init(IAgent me)
    {

    }

    override public IEnumerable<INode>? Children => new List<INode>() { State };

    public RemoteNodes State { get; }
}

public class RemoteNodes : BaseDirectoryNode
{
    public RemoteNodes() : base("state")
    {
    }
}