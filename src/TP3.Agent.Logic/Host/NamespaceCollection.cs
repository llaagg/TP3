using System.Collections.Generic;
using System.Linq;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public sealed class NamespaceCollection
{
    private readonly List<IConnection> connections = new();

    public NamespaceCollection(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }

    public IReadOnlyCollection<IConnection> Connections => connections.AsReadOnly();

    public void AddConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        if (!connections.Contains(connection))
        {
            connections.Add(connection);
        }
    }

    public bool RemoveConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        return connections.Remove(connection);
    }

    public bool ContainsConnection(IConnection connection)
    {
        if (connection is null) throw new ArgumentNullException(nameof(connection));
        return connections.Contains(connection);
    }
}
