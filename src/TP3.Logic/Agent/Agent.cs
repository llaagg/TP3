using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Agent;

/// <summary>
/// Long time thread, listens for incoming connections and handles them.
/// Delivers trunk to the clients connected to it. (that should be other agents)
/// Connects to other agents and requests trunk from them.  
/// </summary>
public class Agent : IAgent
{
    private readonly IRouter router;
    private readonly ILogger? logger;
    private NetworkManager NetworkManager;
    private ServiceManager ServiceManager;

    public Agent(ILogger? logger = null, IService[]? services = null)
    {
        this.logger = logger;
        this.router = new Router(this, logger);
        this.NetworkManager = new NetworkManager(this.router, logger);
        this.ServiceManager = new ServiceManager(this, services ?? Array.Empty<IService>(), logger);
        this.MessageHandler = new MessageHandler(this, this.router, this.T, logger);
    }

    public List<IService> Services { get; private set; } = new List<IService>();
    public MessageHandler MessageHandler { get; private set; }

    public async Task Init()
    {
        await this.NetworkManager.Init();
        await this.ServiceManager.Init();
    }

    public async Task Start()
    {
        await ServiceManager.Start();
    }

    public async Task Stop()
    {
        await ServiceManager.Stop();
    }

    public INode T
    {
        get
        {
            return new Trunk(this.Services);
        }
    }

    public virtual void Dispose()
    {
        this.NetworkManager?.Dispose();
    }

    public async Task Handle(
            INetworkPipe incomingTransport,
            TP3Message request)
    {
        logger?.LogDebug("Agent handling TP3 message: {Command}", request.ToString());

        if (request == null)
        {
            logger?.LogWarning("Received null TP3 message.");
            return;
        }

        try
        {
            await MessageHandler.Handle(incomingTransport, request);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling TP3 message: {Command}", request.ToString());
            await router.Respond(this, MessageHandler.CreateErrorMessage(request.Tag, ex.Message, ex.ToString()), incomingTransport);
        }
    }

    public async Task AddTransport(INetworkTransport transport)
    {
        await this.NetworkManager.AddTransport(transport);
    }
}
