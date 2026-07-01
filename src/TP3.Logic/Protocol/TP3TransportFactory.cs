using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public class TP3TransportFactory
{
    
    public static TP3Transport CreateTCP(int port, IRouter router, ILogger? logger = null)
    {
        INetworkTransport transport = new TCPTransport(port, router, logger);

        return new TP3Transport(transport, logger);
    }

    public static TP3Transport CreateIPC(int port, IRouter router, ILogger? logger = null)
    {
        INetworkTransport transport = new IpcTransport(port, router, logger);

        return new TP3Transport(transport, logger);
    }
}