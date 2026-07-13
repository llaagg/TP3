using TP3.Interfaces;
using TP3.Protocol;
namespace TP3.Service.Shell;

public class ShellService : BaseDirectoryNode, IService
{
    public ShellService()
        : base("shell", null, new List<INode>()
        {
            new BaseDirectoryNode("control", null, new List<INode>()
            {
                new Ls(),
                new Cd(),
                new Sh()
            })
        })
    {
    }

    public void Dispose()
    {
    }

    public async Task Init(IAgent me)
    {
    }

    public async Task Start()
    {
    }

    public async Task Stop()
    {
    }


}

internal class Sh : BaseControlCommand
{
    private ShellService shellService;

    public Sh()
    {
        this.shellService = null;
    }

    protected override async Task HandleStreamCommand(Stream input, Stream output)
    {
        // say hi
        
        // show prompt
        using var writer = new StreamWriter(output, leaveOpen: true);
        await writer.WriteLineAsync("Welcome to the shell!");

        while (true)
        {
            await writer.WriteAsync("$ ");
            await writer.FlushAsync();
            // wait for command and enter
            using var reader = new StreamReader(input, leaveOpen: true);
            var command = await reader.ReadLineAsync();
            
            

            // wait for some commands
            // behave like bash
            // execute actons
            // show prompt again
        }
        // wait for some commands
        // behave like bash
        // execute actons
        // show prompt again

        
    }
}

internal class Cd : BaseControlCommand
{
    private ShellService shellService;

    public Cd()
    {
        this.shellService = null;
    }
}

internal class Ls : BaseControlCommand
{
    private ShellService shellService;

    public Ls()
    {
        this.shellService = null;
    }
}