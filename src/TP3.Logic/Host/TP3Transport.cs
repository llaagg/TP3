using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

public class TP3Transport : ITP3Transport
{
    private ILogger? logger;
    private IRouter? router = null!;
    private INetworkTransport networkTransport;
    private INetworkManager networkManager;

    /// <summary>
    /// Identifies transport instance, used to route messages to the correct transport.
    /// </summary>
    public string TransportTag { get; }

    public TP3Transport(ILogger? logger, INetworkTransport networkTransport)
    {
        this.logger = logger;
        this.networkTransport = networkTransport;
        this.TransportTag = networkTransport.GetType().Name + "_" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    public async Task Send(INetworkPipe session, TP3Message message)
    {
        // thorws stuff into the network channel
        await networkTransport.Send(session, message);
    }

    public async Task Init(INetworkManager networkManager, IRouter router)
    {
        this.networkManager = networkManager;
        this.router = router;
        await this.networkTransport.Init(this);
        logger?.LogInformation("TP3Transport initialized with router.");
    }

    public void Dispose()
    {
        networkTransport.Dispose();
        logger?.LogInformation("TP3Transport disposed.");
    }

    public void NewUserNetworkConnection(INetworkPipe session)
    {
        this.networkManager.NetworkSessions.AddSession(session);
    }

    public INode GetNode(INetworkPipe incomingTransport, string tag)
    {
        INode result = this.networkManager.NetworkSessions.FindNode(incomingTransport, tag);

        return result;
    }

    public void AttachTag(string tag, INode rootNode, INetworkPipe incomingNetworkSession)
    {
        this.networkManager.NetworkSessions.AttachTagToPointer(tag, rootNode, incomingNetworkSession);
    }

    public async Task<ITP3DataStream> GetData(INetworkPipe incomingTransport, string tag)
    {
        var pointer = this.networkManager.NetworkSessions.GetPointer(incomingTransport, tag);
        if (pointer == null)
        {
            throw new Exception($"No pointer found for tag: {tag}");
        }

        if(pointer.Data == null)
        {
            pointer.Data = await pointer.Node.Get();
        }

        if(pointer.Data == null && pointer.Node.NodeType == NodeType.Directory)
        {
            // for directory we can create a stream on the fly
            pointer.Data = new TP3DirectoryStreamData(pointer.Node);
        }

        if(pointer.Data == null)
        {
            throw new Exception($"No data stream available for tag: {tag}");
        }

        await pointer.Data.Open();

        return pointer.Data!;
    }

    public IPointer GetPointer(INetworkPipe incomingTransport, string tag)
    {
        var pointer = this.networkManager.NetworkSessions.GetPointer(incomingTransport, tag);
        if (pointer == null)
        {
            throw new Exception($"No pointer found for tag: {tag}");
        }

        return pointer;
    }

    public async Task ClosePointer(INetworkPipe incomingTransport, string tag)
    {
        this.networkManager.NetworkSessions.CloseSession(incomingTransport, tag);
    }

    public Task Route(INetworkPipe session, TP3Message message)
    {
        return this.router!.Route(session, message);
    }
}
            