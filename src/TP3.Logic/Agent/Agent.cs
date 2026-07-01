using System.Linq;
using Microsoft.Extensions.Logging;
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

    public Agent(IRouter router, ILogger? logger = null)
    {
        this.router = router;
        this.logger = logger;

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


    public async Task Handle(TP3Message request)
    {
        logger?.LogDebug("Agent handling TP3 message: {Command} {Path}", request.Command, string.Join(" ", request.Args));
        if (request == null)
        {
            logger?.LogWarning("Received null TP3 message.");
            return;
        }
        
        if (request.Command == TP3Command.WALK)
        {
            var walkRequest = TP3WalkRequest.From(request);
            var service = ResolvePathService(request.Args);
            if (service is null)
            {
                await Respond(request, new TP3WalkResponse
                {
                    Args = request.Args,
                    Tag = request.Tag,
                    Error = "NotFound"
                });
                return;
            }

            var response = await service.WalkAsync(walkRequest).ConfigureAwait(false);
            await Respond(request, response).ConfigureAwait(false);
            return;
        }

        if (request.Command == TP3Command.READ)
        {
            var readRequest = TP3ReadRequest.From(request);
            IPathDataService? service = null;
            if (!string.IsNullOrWhiteSpace(request.Qid))
            {
                service = ResolveQidService(request.Qid);
            }

            service ??= ResolvePathService(request.Args);
            if (service is null)
            {
                await Respond(request, new TP3ReadResponse
                {
                    Args = request.Args,
                    Tag = request.Tag,
                    Error = "NotFound"
                });
                return;
            }

            var response = await service.ReadAsync(readRequest).ConfigureAwait(false);
            await Respond(request, response).ConfigureAwait(false);
            return;
        }

    }

    private IPathDataService? ResolvePathService(IReadOnlyList<string> requestPath)
    {
        return Services.OfType<IPathDataService>().FirstOrDefault(s => s.CanHandlePath(requestPath));
    }

    private IPathDataService? ResolveQidService(string qid)
    {
        return Services.OfType<IPathDataService>().FirstOrDefault(s => s.CanHandleQid(qid));
    }

    private async Task Respond(TP3Message request, TP3Message tP3Message)
    {
        await router.Respond(this, request, tP3Message);
    }
}
