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
/// I route messages to router.
/// I filter messages with namespaces.
/// </summary>
public class AgentHost : IDisposable
{
    private readonly IAgent agent;
    private readonly ITP3Transport tp3Transport;
    private readonly ITP3Transport ipcTransport;
    private readonly IService[] services;
    private readonly ILogger? logger;
    private readonly Router router;

    public AgentHost(int port = 5000, int ipcPort = 5001, ILogger? logger = null, IService[]? services = null)
    {
        this.logger = logger;

        agent = new Agent.Agent(logger);
        router = new Router(this, logger);

        tp3Transport = TP3Transport.Create(port, async message => await HandleTP3Message(message), logger);
        ipcTransport = TP3Transport.Create(ipcPort, HandleTP3Message, logger, useIpc: true);

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

        await tp3Transport.Start();
        await ipcTransport.Start();
    }

    public IAgent Me => agent;

    public void Dispose()
    {
        logger?.LogInformation("Disposing agent host.");
        ipcTransport.Dispose();
        tp3Transport.Dispose();
    }

    private async Task HandleTP3Message(TP3Message message)
    {
        logger?.LogInformation("Received TP3 message: {Message}", message.Command);
        await router.Route(message);
    }

    public void Stop()
    {
        tp3Transport.Stop();
        ipcTransport.Stop();
    }
}
