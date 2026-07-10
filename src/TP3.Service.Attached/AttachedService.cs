using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Service.Attached;

public class AttachedService: BaseDirectoryNode, IService
{
    private readonly ILogger logger;

    public AttachedService(int port, ILogger logger)
        : base($"attached")
    {
        this.State = new StateNode("state");
        this.logger = logger;
    }

    public INode State { get; private set; } = null!;

    public INode Control { get; private set; } = null!;

    public INode Events { get; private set; } = null!;

    override public IEnumerable<INode>? Children => new INode[] { State };

    public void Dispose()
    {
    }

    public async Task Init(IAgent me)
    {
    }

    public async Task Start()
    {
        while(true)
        {
            this.logger?.LogInformation("AttachedService running...");
            await Task.Delay(1000);
        }
    }

    public async Task Stop()
    {
    }
}

internal class StateNode : BaseDirectoryNode
{
    public StateNode(string name) : base(name)
    {
        this.Tcp = new TcpStateNode("tcp");
    }

    public INode Tcp { get; private set; } = null!;

    override public IEnumerable<INode>? Children => new INode[] { Tcp };
}

public class TcpStateNode : BaseDirectoryNode
{
    public TcpStateNode(string name) : base(name)
    {
        
    }

    override public IEnumerable<INode>? Children => Array.Empty<INode>();
}