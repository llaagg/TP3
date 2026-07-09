using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;

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
public class AgentHost : IAgentHost, IDisposable
{
    public readonly IAgent Me;
    private readonly IService[] services;
    private readonly ILogger? logger;
    public readonly Router router;

    private NetworkSessions networkSessions = new NetworkSessions();
    private List<TP3Transport> transports = new List<TP3Transport>();

    public INetworkSessions NetworkSessions => networkSessions;

    public AgentHost(ILogger? logger = null, IService[]? services = null)
    {
        this.logger = logger;

        router = new Router(this, logger);
        Me = new Agent.Agent(router, logger);

        this.services = services ?? Array.Empty<IService>();
    }


    public async Task Init()
    {
        if (this.transports != null)
        {
            // check if tags are uniq in tranbsports
            var tags = transports.Select(t => t.TransportTag).ToList();
            if (tags.Count != tags.Distinct().Count())
            {
                throw new Exception("Transport tags are not unique.");
            }

            foreach (var t in transports)
            {
                await t.Init(this, router);
            }

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
            
            // let's add just another one
        }

        await Me.AddService(new AgentService(this, logger, this.NetworkSessions));

    }

    public async Task Start()
    {
        logger?.LogInformation("Starting agent host.");

        if (transports == null || transports.Count == 0)
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
