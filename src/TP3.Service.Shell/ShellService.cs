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
        // so listen to all shell instream
        // and list to all shell out streams

        //now soem shceduling might happen here
        //maybe switching between streams and so on

    }

    public async Task Stop()
    {
    }

    public async Task<string> AddNewShell()
    {
        // we can connect to those streams and use tham as cli terminal
        // we will have some screen buffer and all the nice things
        string shortGuid = Guid.NewGuid().ToString().Substring(0, 8);
        var terminalName = $"terminal-{shortGuid}";
        var terminalNode = new Shell(terminalName);
        this.stateNode.AddChild(terminalNode);

        await this.StartListeningToInStream(terminalNode);

        return terminalName;
    }

    private async Task StartListeningToInStream(Shell terminalNode)
    {
        // list to incomig stuff
        var inStream = terminalNode.inStream;
        var outStream = terminalNode.outStream;

        var buffer = new byte[1024];
        while (true)
        {
            int bytesRead = await inStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                // End of stream
                break;
            }
        }
    }
}
