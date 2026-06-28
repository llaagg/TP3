using TP3.Interfaces;

namespace TP3.Client;

public class LocalConnection : IConnection
{
    private readonly IAgent agent;

    public LocalConnection(IAgent agent)
    {
        this.agent = agent;
    }

    public void Connect()
    {
        // Connect to local agent
    }
}
