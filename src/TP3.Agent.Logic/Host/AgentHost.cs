using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using AgentType = TP3.Agent.Logic.Agent.Node;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public class AgentHost : IDisposable
{
    private readonly AgentType agent;
    private readonly TCPTransport tcpTransport;
    private readonly IpcTransport ipcTransport;
    private readonly PeerConnectionManager peerConnectionManager;
    private readonly ILogger? logger;
    private readonly Router router;
    private readonly List<NamespaceCollection> namespaces = new();

    public AgentHost(AgentType agent, int port = 5000, int ipcPort = 5001, ILogger? logger = null)
    {
        this.agent = agent ?? throw new ArgumentNullException(nameof(agent));
        this.logger = logger;
        peerConnectionManager = new PeerConnectionManager(logger);
        router = new Router(this, logger);

        logger?.LogInformation("Starting agent host on port {Port}.", port);
        tcpTransport = new TCPTransport(port, router.Route, logger);

        logger?.LogInformation("Starting IPC host on port {Port}.", ipcPort);
        ipcTransport = new IpcTransport(ipcPort, HandleIpcRequest, logger);
    }

    public AgentType Me => agent;

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

    public static AgentHost Main(string[] args, int port = 5000, int ipcPort = 5001, ILogger? logger = null)
    {
        logger?.LogInformation("Starting agent host...");
        var finalPort = ParsePort(args, port, logger);
        var agent = new AgentType(logger);
        var agentHost = new AgentHost(agent, finalPort, ipcPort, logger);
        return agentHost;
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

    private static int ParsePort(string[] args, int defaultPort, ILogger? logger)
    {
        if (args is null || args.Length == 0)
        {
            return defaultPort;
        }

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--port" || arg == "-p")
            {
                if (i + 1 >= args.Length)
                {
                    logger?.LogWarning("Missing value for {Argument}. Using default port {Port}.", arg, defaultPort);
                    break;
                }

                if (int.TryParse(args[i + 1], out var parsedPort))
                {
                    return parsedPort;
                }

                logger?.LogWarning("Invalid port value '{PortValue}' for {Argument}. Using default port {Port}.", args[i + 1], arg, defaultPort);
                break;
            }

            const string portPrefix = "--port=";
            if (arg.StartsWith(portPrefix, StringComparison.Ordinal))
            {
                var value = arg[portPrefix.Length..];
                if (int.TryParse(value, out var parsedPort))
                {
                    return parsedPort;
                }

                logger?.LogWarning("Invalid port value '{PortValue}' for {Argument}. Using default port {Port}.", value, "--port", defaultPort);
                break;
            }
        }

        return defaultPort;
    }
}
