using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Agent;
using TP3.Agent.Logic.Protocol;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Host;

/// <summary>
/// I know all.
/// I know tcp.
/// I know ipc.
/// I allow to talk to me from my thread.
/// I do not route messages to router.
///    I let router do it.
/// I filter messages with namespaces. 
///    TODO: implement namespace filtering
/// </summary>
public class AgentHost : IDisposable
{
    public readonly IAgent Me;
    private readonly ITP3Transport tcpTransport;
    private readonly ITP3Transport ipcTransport;
    private readonly IService[] services;
    private readonly ILogger? logger;
    private readonly Router router;

    public AgentHost(int port = 5000, int ipcPort = 5001, ILogger? logger = null, IService[]? services = null)
    {
        this.logger = logger;

        router = new Router(this, logger);
        Me = new Agent.Agent(router, logger);

        tcpTransport = TP3TransportFactory.CreateTCP(port, router, logger);
        ipcTransport = TP3TransportFactory.CreateIPC(ipcPort, router, logger);
        this.services = services ?? Array.Empty<IService>();
    }

    public async Task Start()
    {
        logger?.LogInformation("Starting agent host.");

        foreach (var service in services)
        {
            logger?.LogInformation("Initializing service: {ServiceName}", service.GetType().Name);
            try
            {
                await Me.AddService(service);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to initialize service: {ServiceName}", service.GetType().Name);
            }
        }

        var tcpTask = tcpTransport.Start();
        var ipcTask = ipcTransport.Start();

        await Task.WhenAll(tcpTask, ipcTask);
    }

    public void Dispose()
    {
        logger?.LogInformation("Disposing agent host.");
        tcpTransport.Dispose();
        ipcTransport.Dispose();
    }

    public void Stop()
    {
        tcpTransport.Stop();
        ipcTransport.Stop();
    }
}
