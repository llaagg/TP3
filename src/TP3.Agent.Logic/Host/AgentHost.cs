using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using AgentType = TP3.Agent.Logic.Agent.Node;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public class AgentHost : IDisposable
{
    private readonly AgentType agent;
    private readonly TCPTransport tcpTransport;
    private readonly ILogger? logger;
    private readonly PeerConnectionManager peerConnectionManager;
    private readonly Router router;
    private readonly List<NamespaceCollection> namespaces = new();

    public AgentHost(AgentType agent, int port = 5000, ILogger? logger = null)
    {
        this.agent = agent ?? throw new ArgumentNullException(nameof(agent));
        this.logger = logger;
        peerConnectionManager = new PeerConnectionManager(logger);
        router = new Router(this, logger);

        logger?.LogInformation("Starting agent host on port {Port}.", port);
        tcpTransport = new TCPTransport(port, router.Route, logger);
    }

    public AgentType Me => agent;

    public IReadOnlyCollection<IConnection> PeerConnections => peerConnectionManager.Connections;
    public IReadOnlyCollection<NamespaceCollection> Namespaces => namespaces.AsReadOnly();

    public void AddPeerConnection(IConnection connection)
    {
        peerConnectionManager.AddConnection(connection);
    }

    public bool RemovePeerConnection(IConnection connection)
    {
        return peerConnectionManager.RemoveConnection(connection);
    }

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
        tcpTransport.Dispose();
        agent.Dispose();
    }

    public static AgentHost Main(string[] args, int port = 5000, ILogger? logger = null)
    {
        logger?.LogInformation("Starting agent host...");
        var agent = new AgentType(logger);
        var agentHost = new AgentHost(agent, port, logger);
        return agentHost;
    }
}
