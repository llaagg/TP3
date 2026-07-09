using TP3.Interfaces;

namespace TP3.Service.IPC;

public class StateNode : BaseDirectoryNode
{
    private IpcService service;

    public StateNode(IpcService service) : base("state")
    {
        this.service = service;
        
        // who is connected
        this.service.transport.
    }

    override public IEnumerable<INode>? Children => new List<INode>();
    {
       
    };
}