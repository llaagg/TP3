using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Transport;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public sealed class TP3Transport : ITP3Transport
{
    private readonly INetworkTransport transport;
    private readonly ILogger? logger;

    public TP3Transport(int port, Func<TP3Message, string> messageHandler, ILogger? logger = null, bool useIpc = false)
    {
        this.logger = logger;
        transport = useIpc
            ? new IpcTransport(port, rawRequest => messageHandler(TP3Protocol.Parse(rawRequest)), logger)
            : new TCPTransport(port, rawRequest => messageHandler(TP3Protocol.Parse(rawRequest)), logger);
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
