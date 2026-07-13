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
                new Sh(),
                new Create(),
            })
        })
    {
    }

    public void Dispose()
    {
    }

    public async Task Init(IAgent me)
    {
        foreach (var child in Children)
        {
            if (child.Name == "control" )
            {
                var controlNode = child as BaseDirectoryNode;
                foreach (var service in controlNode.Children)
                {
                    if (service is Create c)
                    {
                        c.Init(me);
                    }
                }
            }
        }
    }

    public async Task Start()
    {
    }

    public async Task Stop()
    {
    }


}

public class Create : BaseControlCommand
{
    private ShellService shellService;

    public Create()
    {
    }

    internal void Init(IAgent me)
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
        // read whatever they are saying
        using var reader = new StreamReader(input, leaveOpen: true);
        using var writer = new StreamWriter(output, leaveOpen: true);
        var line = await reader.ReadLineAsync();

        // show prompt
        // let' show nice prompt moth with frames using ascci and info about tp3 (three plus 3 using empotes)
        var motd = @"
          ╭──────────────────────────────────────────────────────────╮
          │                                                          │
          │                   ░▒▓  🌳+3  ▓▒░                         │
          │                                                          │
          │                     *** TP3 Bash ***                     │
          │                                                          │
          ╰──────────────────────────────────────────────────────────╯

        ";
        await writer.WriteLineAsync(motd);
        await writer.FlushAsync();

        while (true)
        {
            await writer.WriteAsync("$ ");
            await writer.FlushAsync();


            // wait for some data comming in to the steam
            line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }            
        }
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