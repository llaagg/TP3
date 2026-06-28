using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public class AgentHost : IDisposable
{
    private readonly Agent agent;
    private readonly TCPTransport tcpTransport;
    private readonly ILogger? logger;
    private readonly List<IConnection> peerConnections = new();
    private readonly List<NamespaceCollection> namespaces = new();

    public AgentHost(Agent agent, int port = 5000, ILogger? logger = null)
    {
        this.agent = agent ?? throw new ArgumentNullException(nameof(agent));
        this.logger = logger;

        logger?.LogInformation("Starting agent host on port {Port}.", port);
        tcpTransport = new TCPTransport(port, agent.HandleRequest, logger);
    }

    public Agent Me => agent;

    public IReadOnlyCollection<IConnection> PeerConnections => peerConnections.AsReadOnly();
    public IReadOnlyCollection<NamespaceCollection> Namespaces => namespaces.AsReadOnly();

    public void AddPeerConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        peerConnections.Add(connection);
        logger?.LogInformation("Added peer connection: {Connection}", connection.GetType().Name);
    }

    public void RemovePeerConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        peerConnections.Remove(connection);
        logger?.LogInformation("Removed peer connection: {Connection}", connection.GetType().Name);
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
        var agent = new Agent(logger);
        var agentHost = new AgentHost(agent, port, logger);
        return agentHost;
    }
}
