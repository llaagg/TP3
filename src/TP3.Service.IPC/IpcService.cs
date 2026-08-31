using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Protocol.Base;

namespace TP3.Service.IPC;

public class IpcService : BaseDirectoryNode, IService
{
    public IpcService(int port, ITP3Logger? logger) : 
        base()
    {
        this.Port = port;
        this.logger = logger;

        this.State = new StateNode(this);
    }

    public int Port { get; }

    private ITP3Logger? logger;
    public IpcTransport? transport = null;

    public override IEnumerable<INode>? Children => new List<INode>{ this.State };

    public StateNode State { get; private set; }

    public async Task Init(IAgent me)
    {
        this.logger?.LogInformation("Initializing IPC service...");

        this.transport = new IpcTransport(this.Port, this.logger);

        await me.AddTransport(this.transport);
    }

    public async Task Start()
    {
        this.logger?.LogInformation("Starting IPC service...");
        if(this.transport!=null)
        {
            await transport.Start();
        }
    }

    public async Task Stop()
    {
        this.logger?.LogInformation("Stopping IPC service...");
        this.transport?.Stop();
    }

    public void Dispose()
    {
        this.transport?.Dispose();
    }
}
