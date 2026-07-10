using Microsoft.Extensions.Logging;
using TP3.Interfaces;
using TP3.Protocol;

namespace TP3.Service.Attached;

public class AttachedService: BaseDirectoryNode, IService
{
    private readonly ILogger logger;
    private readonly int port;
    private TcpServerTransport? transport;

    public AttachedService(int port, ILogger logger)
        : base($"attached")
    {
        this.port = port;
        this.State = new StateNode("state", () => this.transport?.Describe() ?? $"TCP server not started on port {this.port}");
        this.logger = logger;
    }

    public INode State { get; private set; } = null!;

    public INode Control { get; private set; } = null!;

    public INode Events { get; private set; } = null!;

    override public IEnumerable<INode>? Children => new INode[] { State };

    public void Dispose()
    {
        this.transport?.Dispose();
    }

    public async Task Init(IAgent me)
    {
        this.logger?.LogInformation("Initializing attached TCP server on port {Port}...", this.port);
        this.transport = new TcpServerTransport(this.port, this.logger);
        await me.AddTransport(this.transport);
    }

    public async Task Start()
    {
        this.logger?.LogInformation("Starting attached TCP server...");

        if (this.transport is null)
        {
            throw new InvalidOperationException("Attached TCP transport is not initialized.");
        }

        await this.transport.Start();
    }

    public async Task Stop()
    {
        this.transport?.Stop();
    }
}

internal class StateNode : BaseDirectoryNode
{
    public StateNode(string name, Func<string> transportDescription) : base(name)
    {
        this.Tcp = new TcpStateNode("tcp", transportDescription);
    }

    public INode Tcp { get; private set; } = null!;

    override public IEnumerable<INode>? Children => new INode[] { Tcp };
}

public class TcpStateNode : BaseDirectoryNode
{
    private readonly Func<string> transportDescription;

    public TcpStateNode(string name, Func<string> transportDescription) : base(name)
    {
        this.transportDescription = transportDescription;
    }

    override public IEnumerable<INode>? Children => new INode[]
    {
        new StreamNode(this.transportDescription, "transport")
    };
}