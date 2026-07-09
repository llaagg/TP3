using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Service.IPC;

public class IpcService : BaseDirectoryNode, IService
{
    public IpcService(int port, ILogger? logger) : 
        base()
    {
        this.Port = port;
        this.logger = logger;
    }

    public int Port { get; }

    private ILogger? logger;
    private IpcTransport transport;

    public async Task Init(IAgent me)
    {
        this.logger?.LogInformation("Initializing IPC service...");

        this.transport = new IpcTransport(this.Port, this.logger);

        me.AddTransport(this.transport);
    }

    public async Task Start()
    {
        this.logger?.LogInformation("Starting IPC service... ipc transport is handled by network manager, no init control here.");
    }

    public async Task Stop()
    {
        this.logger?.LogInformation("Stopping IPC service... ipc transport is handled by network manager, no init control here.");
    }

    public void Dispose()
    {
        this.transport.Dispose();
    }
}
