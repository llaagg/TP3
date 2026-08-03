using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Service.PS;

internal class AutoStart : BaseDirectoryNode, INode
{
    public AutoStart() : base("autostart")
    {
        
    }

    public override IEnumerable<INode>? Children => AutoStartHelper.GetAutoStartApps().ToList();
}
