using Microsoft.Extensions.Logging;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

/// <summary>
/// TCP transport is intentionally disabled for now.
/// IPC transport is the only active transport in the current MVP.
/// </summary>
public sealed class TCPTransport : INetworkTransport
{
    private readonly ILogger? logger;

    public TCPTransport(int port, IRouter router, ILogger? logger = null)
    {
        this.logger = logger;
        this.logger?.LogInformation("TCP transport disabled for MVP. Port {Port} is ignored.", port);
    }

    public Task Start()
    {
        logger?.LogInformation("TCP transport start skipped (disabled).");
        return Task.CompletedTask;
    }

    public void Stop()
    {
        logger?.LogInformation("TCP transport stop skipped (disabled).");
    }

    public Task Send(TP3Message message)
    {
        logger?.LogDebug("TCP transport send skipped (disabled). Command={Command}", message.Command);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
