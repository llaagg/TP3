using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;
using TP3.Protocol.Base;

public class InitService : BaseDirectoryNode, IService
{
    public IAgent me;
    private AutoStart autostartNode;

    public InitService() : base("init-service")
    {
       
    }

    public void Dispose()
    {
    }

    public async Task Init(IAgent me)
    {
        this.me = me;

    }

    public async Task Start()
    {
        this.autostartNode = new AutoStart(this);
        this.AddChild(new BaseDirectoryNode("control", children: new INode[]{ this.autostartNode }));

        // var data = await this.autostartNode.Get();
        // var bytes = await data!.Read(0, 65536);
        
        // System.Console.WriteLine($"InitService: autostart output: {System.Text.Encoding.UTF8.GetString(bytes)}");
    }

    public async Task Stop()
    {
    }
}

internal class AutoStart : BaseControlParamsArgsCommand
{
    public AutoStart(InitService initService) 
    {
        InitService = initService;
    }

    public InitService InitService { get; }

    // Removed redundant empty block

    protected override async Task HandleParamsArgsCommand(Stream output, params string[]? args)
    {
        await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Starting.\n"));
        
        // first make /root 
        // let's say we will gonna have mounts, etc, bin, home

        // in bin we will put all commands
        // in etc all configs
        // in homme some folder
        // in mnt some mount points

        // everythin is node anyway so we have full freedon to map however
        // we could have a virtual node map that maps to physical resources

        // so namespaces will be virtual on top of nodees

        // so let's have posix viirtual nodes as first

        // by default everything is in services anyway

        this.CreatePosixNamespace();

        await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Done.\n"));
    }

    private void CreatePosixNamespace()
    {
        var trunk = this.InitService.me.GetTrunk();
        var namespaces = trunk.Children!.FirstOrDefault(c => c.Name == "name-spaces") as BaseDirectoryNode;

        // override posix node
        var posixNode = new PosixNode();
        namespaces?.AddChild(posixNode);
    }
}

public class PosixNode : BaseDirectoryNode
{
    public PosixNode() : base("posix")
    {
        this.AddChild(new BaseDirectoryNode("bin"));
        this.AddChild(new BaseDirectoryNode("etc"));
        this.AddChild(new BaseDirectoryNode("home"));
        this.AddChild(new BaseDirectoryNode("mnt"));
    }
}