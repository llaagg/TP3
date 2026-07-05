using Google.Protobuf;
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
        logger?.LogDebug("Agent handling TP3 message: {Command}", request.ToString());

        if (request == null)
        {
            logger?.LogWarning("Received null TP3 message.");
            return;
        }

        try
        {
            await MainLoop(incomingTransport, request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling TP3 message: {Command}", request.ToString());
            await router.Respond(this, CreateErrorMessage(request.Tag, ex.Message, ex.ToString()), incomingTransport);
        }
    }

    private async Task MainLoop(INetworkPipe incomingTransport, TP3Message request)
    {
        switch (request.PayloadCase)
        {
            case TP3Message.PayloadOneofCase.WalkRequest:
                    await Walk(incomingTransport, request).ConfigureAwait(false);
                    break;
            case TP3Message.PayloadOneofCase.ReadRequest:
                    await Read(incomingTransport, request).ConfigureAwait(false);
                    break;
            case TP3Message.PayloadOneofCase.AttachRequest:
                    await Attach(incomingTransport, request);
                    break;
            case TP3Message.PayloadOneofCase.OpenRequest:
                    await Open(incomingTransport, request); 
                    break;
            case TP3Message.PayloadOneofCase.ClunkRequest:
                    await Clunk(incomingTransport, request);
                    break;
            default:
                    await Unknown(incomingTransport, request);
                    break;
        }
    }

    private async Task Unknown(INetworkPipe incomingTransport, TP3Message request)
    {
        await router.Respond(this, CreateErrorMessage(request.Tag, $"Unhandled TP3 message: {request.AttachRequest}"), incomingTransport);
    }

    private async Task Clunk(INetworkPipe incomingTransport, TP3Message request)
    {
        var tag = request.Tag;
        var response = await Clunk(incomingTransport, tag, request.ClunkRequest);

        await router.Respond(this, CreateMessage(tag, m => m.ClunkResponse = response), incomingTransport);
    }

    private async Task Open(INetworkPipe incomingTransport, TP3Message request)
    {
        var tag = request.Tag;
        var response = await OpenStream(incomingTransport, tag, request.OpenRequest);

        await router.Respond(this, CreateMessage(tag, m => m.OpenResponse = response), incomingTransport);
    }

    private async Task Attach(INetworkPipe incomingTransport, TP3Message request)
    {
        var tag = request.Tag;
        var response = await AttachTagToConnectionAndGetRootGiq(incomingTransport, tag, request.AttachRequest);

        await router.Respond(this, CreateMessage(tag, m => m.AttachResponse = response), incomingTransport);
    }

    private async Task Read(INetworkPipe incomingTransport, TP3Message request)
    {
        var tag = request.Tag;
        var node = incomingTransport.TP3Transport.GetNode(incomingTransport, tag);

        if (node == null)
        {
            await router.Respond(this, CreateErrorMessage(tag, "Node not found."), incomingTransport);
        }
        else
        {
            await ReadDataAndSend(request.ReadRequest, tag, incomingTransport);
        }
    }

    private async Task Walk(INetworkPipe incomingTransport, TP3Message request)
    {
        var tag = request.Tag;

        // if we have new tag, we need to create a new pointer for it, by cloening the current node and attaching it to the new tag
        if (!string.IsNullOrEmpty(request.WalkRequest.NewTag))
        {
            var oldPointer = incomingTransport.TP3Transport.GetPointer(incomingTransport, tag);
            var newTag = request.WalkRequest.NewTag;
            incomingTransport.TP3Transport.AttachTag(newTag, oldPointer.Node, incomingTransport);
            tag = newTag;
        }

        var pointer = incomingTransport.TP3Transport.GetPointer(incomingTransport, tag);
        var nodes = await walker.WalkAsync(request.WalkRequest, pointer.Node).ConfigureAwait(false);

        pointer.Node = nodes.LastOrDefault() ?? pointer.Node;

        var response = new TP3WalkResponse();
        response.Infos.Add(nodes.Select(ToNodeInfo));

        await router.Respond(this, CreateMessage(tag, m => m.WalkResponse = response), incomingTransport);
    }

    private async Task<TP3ClunkResponse> Clunk(INetworkPipe incomingTransport, string tag, TP3ClunkRequest tP3ClunkRequest)
    {
        var pointer = incomingTransport.TP3Transport.GetPointer(incomingTransport, tag);
        if (pointer != null)
        {
            try
            {
                pointer.Data?.Close();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error closing data stream for tag: {Tag}", tag);
            }
            finally
            {
                pointer.Data = null;
            }
        }

        await incomingTransport.TP3Transport.ClosePointer(incomingTransport, tag);

        var response = new TP3ClunkResponse();
        return response;
    }

    public async Task<TP3OpenResponse> OpenStream(INetworkPipe incomingTransport, string tag, TP3OpenRequest tP3OpenRequest)
    {
        // let's find the node in this connection
        if (string.IsNullOrEmpty(tag))
        {
            throw new ArgumentException("Tag cannot be null or empty for open request.");
        }

        var node = incomingTransport.TP3Transport.GetNode(incomingTransport, tag);
        var data = await incomingTransport.TP3Transport.GetData(incomingTransport, tag);

        return new TP3OpenResponse
        {
            Info = ToNodeInfo(node),
            Iounit = data.Iounit,
        };
    }

    public async Task ReadDataAndSend(TP3ReadRequest tP3ReadRequest, string tag, INetworkPipe incomingTransport)
    {
        TP3ReadResponse response = new TP3ReadResponse();

        var pointer = incomingTransport.TP3Transport.GetPointer(incomingTransport, tag);
        if (pointer == null)
        {
            // not attched?
            await router.Respond(this, CreateErrorMessage(tag, "not_attached"), incomingTransport);
            return;
        }
        if (pointer.Data == null)
        {
            // not opened?
            await router.Respond(this, CreateErrorMessage(tag, "not_opened"), incomingTransport);
            return;
        }

        var bytes = await pointer.Data.Read(tP3ReadRequest.Offset, tP3ReadRequest.MaxBytes);
        response.Data = ByteString.CopyFrom(bytes);

        await router.Respond(this, CreateMessage(tag, m => m.ReadResponse = response), incomingTransport);
    }

    private async Task<TP3AttachResponse> AttachTagToConnectionAndGetRootGiq(INetworkPipe incomingNetworkSession, string tag, TP3AttachRequest tP3AttachRequest)
    {
        var result = new TP3AttachResponse();

        // check auth
#warning TODO: auth

        // register Tag
        incomingNetworkSession.TP3Transport.AttachTag(tag, this.T, incomingNetworkSession);

        // get quid
        result.Info = ToNodeInfo(this.T);

        return result;
    }

    private static TP3Message CreateMessage(string tag, Action<TP3Message> payloadSetter)
    {
        var message = new TP3Message
        {
            Tag = tag,
        };

        payloadSetter(message);
        return message;
    }

    private static TP3Message CreateErrorMessage(string tag, string error, params string[] args)
    {
        return new TP3Message
        {
            Tag = tag,
            Error = new TP3Error
            {
                Message = error,
                Args = { args }
            }
        };
    }

    private static NodeInfo ToNodeInfo(INode node)
    {
        return new NodeInfo
        {
            Id = node.Id,
            NodeType = node.NodeType,
        };
    }
}
