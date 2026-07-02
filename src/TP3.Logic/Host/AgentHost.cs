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
    private readonly IService[] services;
    private readonly ITP3Transport[]? transports;
    private readonly ILogger? logger;
    public readonly Router router;

    public AgentHost(ILogger? logger = null, IService[]? services = null, ITP3Transport[]? transports = null)
    {
        this.logger = logger;

        router = new Router(this, logger);
        Me = new Agent.Agent(router, logger);

        this.services = services ?? Array.Empty<IService>();
        this.transports = transports;
    }


    public async Task Init()
    {
        if(this.transports != null)
        {
            foreach (var t in transports)
            {
                await t.Init(router);
            }
        }
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

        if(transports == null || transports.Length == 0)
        {
            logger?.LogWarning("No transports configured for AgentHost.");
        }
        
        List<Task> transportStartTasks = new List<Task>();
        if (transports != null)
        {
            foreach (var t in transports)
            {
                transportStartTasks.Add(t.Start());
            }
        }

        await Task.WhenAll(transportStartTasks);
    }

    public void Dispose()
    {
        logger?.LogInformation("Disposing agent host.");
        if (transports != null)
        {
            foreach (var t in transports)
            {
                t.Dispose();
            }
        }
    }

    public void Stop()
    {
        if (transports != null)
        {
            foreach (var t in transports)
            {
                t.Stop();
            }
        }
    }
}
