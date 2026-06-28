using Microsoft.Extensions.Logging;
using TP3.Interfaces;

namespace TP3.Client;

public class LocalConnection : IConnection
{
    private readonly IAgent agent;
    private readonly ILogger<LocalConnection>? logger;

    public LocalConnection(IAgent agent, ILogger<LocalConnection>? logger = null)
    {
        this.agent = agent;
        this.logger = logger;
    }

    public void Connect()
    {
        // Connect to local agent
    }
}
