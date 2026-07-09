using TP3.Interfaces;

namespace TP3.Service.Remote;

public class ControlNodes : BaseDirectoryNode
{
    public ControlNodes():
        base("control")
    {
    }

    override public IEnumerable<INode>? Children => new List<INode>() { new AttachRemote() };
}
