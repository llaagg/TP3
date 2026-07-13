using TP3.Interfaces;
using TP3.Protocol;
namespace TP3.Service.Shell;

public class Create : BaseControlParamsArgsCommand
{
    private ShellService shellService;

    public Create(ShellService shellService) : base()
    {
        this.shellService = shellService;
        #warning it could create session in state with 2 stream
        #warning and maybe consume stuff from it

        #warning but if we need databases... than hmm
    }

    protected override async Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        string terminalName = await this.shellService.AddNewShell();

        await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes(terminalName));
    }
}
