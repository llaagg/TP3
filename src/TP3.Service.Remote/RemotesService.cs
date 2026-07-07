using TP3.Interfaces;

namespace TP3.Service.Remote;

public class RemotesService : BaseDirectoryNode, IService
{
    public RemotesService() : base()
    {
        this.State = new RemoteNodes();
        this.Control = new ControlNodes();
    }

    public async Task Init(IAgent me)
    {

    }

    override public IEnumerable<INode>? Children => new List<INode>() { State, Control };

    public RemoteNodes State { get; }
    public ControlNodes Control { get; }
}
