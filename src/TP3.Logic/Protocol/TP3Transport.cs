using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public sealed class TP3Transport : ITP3Transport
{
    private readonly INetworkTransport transport;
    private readonly ILogger? logger;

    public TP3Transport(INetworkTransport transport, ILogger? logger = null)
    {
        this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.logger = logger;
    }

    public static TP3Transport Create(int port, Func<TP3Message, string> messageHandler, ILogger? logger = null, bool useIpc = false)
    {
        if (messageHandler is null)
        {
            throw new ArgumentNullException(nameof(messageHandler));
        }

        INetworkTransport transport = useIpc
            ? new IpcTransport(port, rawRequest => messageHandler(TP3Protocol.Parse(rawRequest)), logger)
            : new TCPTransport(port, rawRequest => messageHandler(TP3Protocol.Parse(rawRequest)), logger);

        return new TP3Transport(transport, logger);
    }

    public Task Start()
    {
        return transport.Start();
    }

    public Task PublishEventAsync(string eventText)
    {
        return transport.PublishEventAsync(eventText);
    }

    public void Stop()
    {
        transport.Stop();
    }

    public void Dispose()
    {
        transport.Dispose();
    }
}
