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

            await router.Respond(this, response, incomingTransport);
            return;
        }
        else if (request.Command == TP3Command.READ && request is TP3ReadRequest tP3ReadRequest)
        {
            var node = incomingTransport.TP3Transport.GetNode(incomingTransport, tP3ReadRequest.Tag);
            ;


            if(node != null)
            {
                await SendToNetwork(node, tP3ReadRequest, incomingTransport);
            }
            else
            {
                logger?.LogError("No node found for tag: {Tag} in session: {AgentID}", tP3ReadRequest.Tag, incomingTransport.AgentID);
            }
            
            return;
        }
        else if (request.Command == TP3Command.ATTACH && request is TP3AttachRequest tP3AttachRequest)
        {
            var response = await AttachTagToConnectionAndGetRootGiq(incomingTransport, tP3AttachRequest);

            await router.Respond(this, response, incomingTransport);
            return;
        }

        logger?.LogWarning("Agent received unhandled TP3 message: {Command}", request.Command);
    }

    private async Task SendToNetwork(INode nodeObj, TP3ReadRequest tP3ReadRequest, INetworkPipe incomingTransport)
    {
        TP3Message response = null!;


        // let's find node from our session identified by tag
        // le'ts offset and 
        // let's send it back to customer, one or many messages
        if(nodeObj.NodeType == NodeType.Directory)
        {
            response = this.SendDirecotryToNetwork(nodeObj, tP3ReadRequest, incomingTransport);
        }else
        {
            throw new NotImplementedException("File node reading is not implemented yet.");
        }
        
        await router.Respond(this, response, incomingTransport);
    }

    private TP3Message SendDirecotryToNetwork(INode nodeObj, TP3ReadRequest tP3ReadRequest, INetworkPipe incomingTransport)
    {


        //// the idea is not that simple
        var response = new TP3ReadResponse
        {
            Tag = tP3ReadRequest.Tag,
        };
        var offset = tP3ReadRequest.Offset;
        
        logger?.LogError("Node object is a directory for tag: {Tag} in session: {AgentID}", tP3ReadRequest.Tag, incomingTransport.AgentID);
        
        return response;
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
        incomingNetworkSession.TP3Transport.AttachTag(tP3AttachRequest.Tag, rootNode, incomingNetworkSession);

        // get quid
        result.Info = new NodeInfo(rootNode); 


        return result;
    }
}
