using System.Linq;
using Microsoft.Extensions.Logging;
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
    private readonly PathWalker walker;

    public Agent(IRouter router, ILogger? logger = null)
    {
        this.router = router;
        this.logger = logger;
        this.walker = new PathWalker(this.T);

        logger?.LogInformation("Initializing agent logic.");
    }

    public INode T
    {
        get
        {
            return new Trunk(this.Services);
        }
    }

    public List<IService> Services { get; private set; } = new List<IService>();

    public async Task AddService(IService service)
    {
        try
        {
            await service.Init(this);
            this.Services.Add(service);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to initialize service: {service.GetType().Name}", ex);
        }
    }

    public virtual void Dispose()
    {
        // Agent logic has no transport of its own.
    }


    public async Task Handle(
            INetworkPipe incomingTransport,
            TP3Message request)
    {
        logger?.LogDebug("Agent handling TP3 message: {Command}", request.Command);
        if (request == null)
        {
            logger?.LogWarning("Received null TP3 message.");
            return;
        }
        else if (request.Command == TP3Command.WALK && request is TP3WalkRequest tP3WalkRequest)
        {
            var response = await walker.WalkAsync(tP3WalkRequest).ConfigureAwait(false);

            await router.Respond(this, request, incomingTransport);
            return;
        }
        else if (request.Command == TP3Command.READ && request is TP3ReadRequest tP3ReadRequest)
        {
            var response = await walker.ReadAsync(tP3ReadRequest).ConfigureAwait(false);

            await router.Respond(this, request, incomingTransport);
            return;
        }
        else if (request.Command == TP3Command.ATTACH && request is TP3AttachRequest tP3AttachRequest)
        {
            var response = await AttachTagToConnectionAndGetRootGiq(incomingTransport, tP3AttachRequest);

            await router.Respond(this, request, incomingTransport);
            return;
        }

        logger?.LogWarning("Agent received unhandled TP3 message: {Command}", request.Command);
    }

    private async Task<TP3AttachResponse> AttachTagToConnectionAndGetRootGiq(INetworkPipe incomingNetworkSession, TP3AttachRequest tP3AttachRequest)
    {
        var result = new TP3AttachResponse
        {
            Tag = tP3AttachRequest.Tag,
        };

        // check auth
        #warning TODO: auth

        // find root node
        var rootNode = this.T;

        // register Tag
        

        // get quid

        // assing TCP connection to tag

        return result;
    }
}
