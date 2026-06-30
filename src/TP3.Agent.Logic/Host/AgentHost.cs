using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Agent;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public class AgentHost : IDisposable
{
    private readonly Node agent;
    private readonly TCPTransport tcpTransport;
    private readonly IpcTransport ipcTransport;
    private readonly IService[] services;
    private readonly PeerConnectionManager peerConnectionManager;
    private readonly ILogger? logger;
    private readonly Router router;
    private readonly List<NamespaceCollection> namespaces = new();

    public AgentHost(int port = 5000, int ipcPort = 5001, ILogger? logger = null, IService[]? services = null)
    {
        agent = new Node(logger);
        this.logger = logger;
        peerConnectionManager = new PeerConnectionManager(logger);
        router = new Router(this, logger);

        tcpTransport = new TCPTransport(port, router.Route, logger);
        ipcTransport = new IpcTransport(ipcPort, HandleIpcRequest, logger);

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
                await service.Init(Me);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to initialize service: {ServiceName}", service.GetType().Name);
            }
        }

        await tcpTransport.Start();
        await ipcTransport.Start();
    }

    public Node Me => agent;

    public IReadOnlyCollection<IConnection> PeerConnections => peerConnectionManager.Connections;
    public IReadOnlyCollection<NamespaceCollection> Namespaces => namespaces.AsReadOnly();

    public NamespaceCollection CreateNamespace(string name)
    {
        var ns = new NamespaceCollection(name);
        namespaces.Add(ns);
        logger?.LogInformation("Created namespace: {Namespace}", name);
        return ns;
    }

    public bool RemoveNamespace(NamespaceCollection ns)
    {
        if (ns is null) throw new ArgumentNullException(nameof(ns));
        var removed = namespaces.Remove(ns);
        if (removed)
        {
            logger?.LogInformation("Removed namespace: {Namespace}", ns.Name);
        }

        return removed;
    }

    public NamespaceCollection? GetNamespace(string name)
    {
        return namespaces.Find(ns => ns.Name == name);
    }

    public void PublishEvent(string eventText)
    {
        logger?.LogInformation("Publishing event: {EventText}", eventText);
        tcpTransport.PublishEventAsync(eventText).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        logger?.LogInformation("Disposing agent host.");
        ipcTransport.Dispose();
        tcpTransport.Dispose();
        agent.Dispose();
    }

    private string HandleIpcRequest(string request)
    {
        logger?.LogInformation("Received IPC request: {Request}", request);
        if (request.StartsWith("ECHO ", StringComparison.OrdinalIgnoreCase))
        {
            var message = request.Substring(5);
            return message;
        }

        return $"Unknown IPC command: {request}";
    }

    public void Stop()
    {
        tcpTransport.Stop();
        ipcTransport.Stop();
    }
}
