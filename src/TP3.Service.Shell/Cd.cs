using TP3.Protocol;
namespace TP3.Service.Shell;

internal class Cd : BaseControlCommand
{
    private ShellService shellService;

    public Cd()
    {
        this.shellService = null;
    }
}
