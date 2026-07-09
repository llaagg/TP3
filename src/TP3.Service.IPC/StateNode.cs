using TP3.Interfaces;
using TP3.Protocol;

namespace TP3.Service.IPC;

public class StateNode : BaseDirectoryNode
{
    private IpcService service;

    public StateNode(IpcService service) : base("state")
    {
        this.service = service;
    }

    override public IEnumerable<INode>? Children => new List<INode>(
        new INode[] {
            new StreamNode(() => this.service.transport?.Describe() ?? "", "transport"),
        }
    );
}

