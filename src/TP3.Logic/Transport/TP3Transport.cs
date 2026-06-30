using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Protocol;
using TP3.Interfaces;

namespace TP3.Agent.Logic.Host;

public sealed class TP3Transport : ITP3Transport
{
    private readonly TCPTransport tcpTransport;
    private readonly ILogger? logger;

    public TP3Transport(int port, Func<TP3Message, string> messageHandler, ILogger? logger = null)
    {
        this.logger = logger;
        tcpTransport = new TCPTransport(port, rawRequest => messageHandler(TP3Protocol.Parse(rawRequest)), logger);
    }

    public Task Start()
    {
        return tcpTransport.Start();
    }

    public Task PublishEventAsync(string eventText)
    {
        return tcpTransport.PublishEventAsync(eventText);
    }

    public void Stop()
    {
        tcpTransport.Stop();
    }

    public void Dispose()
    {
        tcpTransport.Dispose();
    }
}
