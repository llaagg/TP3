using TP3.Interfaces;
namespace TP3.Service.Shell;

public class ShellService : BaseDirectoryNode, IService
{
    private State stateNode;

    public ShellService()
        : base("shell")
    {
        this.stateNode = new State("state");
    }

    override public IEnumerable<INode>? Children => GetChildren();

    private List<INode> GetChildren() =>
    [
        this.stateNode,
        new BaseDirectoryNode("control", null,
        [
            new Ls(),
            new Cd(),
            new Sh(),
            new Create(this)
        ]),
    ];

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

    public async Task<string> AddNewShell()
    {
        // 2 streams for input and output
        // created under one terminal node

        // we can connect to those streams and use tham as cli terminal
        // we will have some screen buffer and all the nice things

        var terminalName = $"terminal-{Guid.NewGuid()}";
        var terminalNode = new Shell(terminalName);
        this.stateNode.AddChild(terminalNode);
        return terminalName;
    }
}


public class Shell : BaseDirectoryNode
{
    public Shell(string name) : 
        base(name)
    {

    }
}