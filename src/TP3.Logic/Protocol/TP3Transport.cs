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

    public Task Start()
    {
        return transport.Start();
    }

    public Task PublishEventAsync(string eventText)
    {
        return transport.PublishEventAsync(eventText);
    }

    public Task Send(TP3Message message)
    {
        return transport.Send(message);
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
