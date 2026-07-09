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

    public async Task Init(IAgent me)
    {
        this.logger?.LogInformation("Initializing IPC service...");

        //this.transport = new IpcTransport(this.Port, this.logger);
    }
}
