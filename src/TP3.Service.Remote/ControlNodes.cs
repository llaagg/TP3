using TP3.Interfaces;

namespace TP3.Service.Remote;

public class ControlNodes : BaseDirectoryNode
{
    private RemotesService service;

    public ControlNodes(RemotesService remotesService) :
        base("control")
    {
        this.service = remotesService;
    }

    override public IEnumerable<INode>? Children => new List<INode>() { new Attach(this.service) };
}
