using System.Net.Sockets;

namespace TP3.Agent.Logic.Host;

public class Subscriber
{
    public TcpClient Client { get; }
    public NetworkStream Stream { get; }
    public SemaphoreSlim WriteLock { get; } = new(1, 1);

    public Subscriber(TcpClient client, NetworkStream stream)
    {
        Client = client;
        Stream = stream;
    }
}
