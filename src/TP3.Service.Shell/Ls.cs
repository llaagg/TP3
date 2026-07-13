using TP3.Protocol;
namespace TP3.Service.Shell;

internal class Ls : BaseControlCommand
{
    private ShellService shellService;

    public Ls()
    {
        this.shellService = null;
    }
}