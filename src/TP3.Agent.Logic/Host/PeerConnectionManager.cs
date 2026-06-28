using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public sealed class PeerConnectionManager
{
    private readonly List<IConnection> connections = new();
    private readonly ILogger? logger;

    public PeerConnectionManager(ILogger? logger = null)
    {
        this.logger = logger;
    }

    public IReadOnlyCollection<IConnection> Connections => connections.AsReadOnly();

    public void AddConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        if (!connections.Contains(connection))
        {
            connections.Add(connection);
            logger?.LogInformation("Added peer connection: {Connection}", connection.GetType().Name);
        }
    }

    public bool RemoveConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        var removed = connections.Remove(connection);
        if (removed)
        {
            logger?.LogInformation("Removed peer connection: {Connection}", connection.GetType().Name);
        }

        return removed;
    }

    public bool ContainsConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        return connections.Contains(connection);
    }
}
