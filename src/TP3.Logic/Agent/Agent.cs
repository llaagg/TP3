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
        this.walker = new PathWalker();

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
            var pointer = incomingTransport.TP3Transport.GetPointer(incomingTransport, tP3WalkRequest.Tag);
            
            var nodes = await walker.WalkAsync(tP3WalkRequest, pointer.Node).ConfigureAwait(false);

            pointer.Node = nodes!.LastOrDefault()!;

            var response = new TP3WalkResponse
            {
                Tag = tP3WalkRequest.Tag,
                Infos = nodes.Select(n => new NodeInfo(n)).ToList()
            };

            await router.Respond(this, response, incomingTransport);
            return;
        }
        else if (request.Command == TP3Command.READ && request is TP3ReadRequest tP3ReadRequest)
        {
            var node = incomingTransport.TP3Transport.GetNode(incomingTransport, tP3ReadRequest.Tag);

            if(node == null)
            {
                await router.Respond(this, new TP3ErrorResponse ($"Node not found."), incomingTransport);
            }
            else
            {
                await ReadDataAndSend(tP3ReadRequest, incomingTransport);
            }         
            
            return;
        }
        else if (request.Command == TP3Command.ATTACH && request is TP3AttachRequest tP3AttachRequest)
        {
            var response = await AttachTagToConnectionAndGetRootGiq(incomingTransport, tP3AttachRequest);

            await router.Respond(this, response, incomingTransport);
            return;
        }
        else if (request.Command == TP3Command.OPEN && request is TP3OpenRequest tP3OpenRequest)
        {
            var response = await OpenStream(incomingTransport, tP3OpenRequest);
            await router.Respond(this, response, incomingTransport);
            return;
        }else if (request.Command == TP3Command.CLUNK && request is TP3ClunkRequest tP3ClunkRequest)
        {
            TP3ClunkResponse response = Clunk(incomingTransport, tP3ClunkRequest);

            await router.Respond(this, response, incomingTransport);
            return;
        }
        else
        {
            await router.Respond(this, new TP3ErrorResponse($"Unhandled TP3 message: {request.Command}"), incomingTransport);
        }
    }

    private TP3ClunkResponse Clunk(INetworkPipe incomingTransport, TP3ClunkRequest tP3ClunkRequest)
    {
        var pointer = incomingTransport.TP3Transport.GetPointer(incomingTransport, tP3ClunkRequest.Tag);
        if (pointer != null)
        {
            try
            {
                pointer.Data?.Close();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error closing data stream for tag: {Tag}", tP3ClunkRequest.Tag);
            }
            finally
            {
                pointer.Data = null;
            }
        }

        var response = new TP3ClunkResponse(tP3ClunkRequest.Tag);
        return response;
    }

    public async Task<TP3OpenResponse> OpenStream(INetworkPipe incomingTransport, TP3OpenRequest tP3OpenRequest)
    {
        // let's find the node in this connection
        if(string.IsNullOrEmpty(tP3OpenRequest.Tag))
        {
            throw new ArgumentException("Tag cannot be null or empty for open request.");
        }

        var node = incomingTransport.TP3Transport.GetNode(incomingTransport, tP3OpenRequest.Tag);
        var data = await incomingTransport.TP3Transport.GetData(incomingTransport, tP3OpenRequest.Tag);
        
        return new TP3OpenResponse(tP3OpenRequest.Tag, new NodeInfo(node), data.Iounit);        
    }

    public async Task ReadDataAndSend(TP3ReadRequest tP3ReadRequest, INetworkPipe incomingTransport)
    {
        TP3ReadResponse response = new TP3ReadResponse
        {
            Tag = tP3ReadRequest.Tag,
        };

        var pointer = incomingTransport.TP3Transport.GetPointer(incomingTransport, tP3ReadRequest.Tag);
        if(pointer == null)
        {
            // not attched?
            await router.Respond(this, new TP3ErrorResponse($"not_attached"), incomingTransport);
            return;
        }
        if(pointer.Data == null)
        {
            // not opened?
            await router.Respond(this, new TP3ErrorResponse($"not_opened"), incomingTransport);
            return;
        }
        
        var bytes = await pointer.Data.Read(tP3ReadRequest.Offset, tP3ReadRequest.MaxBytes);
        response.Data = bytes;
        
        await router.Respond(this, response, incomingTransport);
    }

    private async Task<TP3AttachResponse> AttachTagToConnectionAndGetRootGiq(INetworkPipe incomingNetworkSession, TP3AttachRequest tP3AttachRequest)
    {
        var result = new TP3AttachResponse
        {
            Tag = tP3AttachRequest.Tag,
        };

        // check auth
        #warning TODO: auth

        // register Tag
        incomingNetworkSession.TP3Transport.AttachTag(tP3AttachRequest.Tag, this.T, incomingNetworkSession);

        // get quid
        result.Info = new NodeInfo(this.T); 
        
        return result;
    }
}
