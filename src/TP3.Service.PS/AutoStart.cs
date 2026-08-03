using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Service.PS;

internal class AutoStart : BaseDirectoryNode, INode
{
    public AutoStart() : base("autostart")
    {
        
    }

    public override IEnumerable<INode>? Children => this.GetAutoStartApps();

    private IEnumerable<INode>? GetAutoStartApps()
    {
        var autoStartApps = AutoStartHelper.GetAutoStartApps();
        foreach (var app in autoStartApps)
        {
            yield return new AutoStartApp(app.Name, app.Path, app.Enabled);
        }
    }
}
