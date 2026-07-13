using TP3.Interfaces;
using TP3.Protocol;
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
        string shortGuid = Guid.NewGuid().ToString().Substring(0, 8);
        var terminalName = $"terminal-{shortGuid}";
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
        this.inStream = new MemoryStream();
        this.outStream = new MemoryStream();

        // two streams in - and out
        this.In = new StreamNode(() => inStream);
        this.Out = new StreamNode(() => outStream);

        this.childs = new List<INode>() { In, Out };
    }

    private Stream inStream;
    private Stream outStream;

    public StreamNode In { get; }
    public StreamNode Out { get; }

    private List<INode> childs;

    override public IEnumerable<INode>? Children => this.childs;
}