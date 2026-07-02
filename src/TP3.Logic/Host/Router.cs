using System;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Protocol;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public sealed class Router : IRouter
{
    private readonly AgentHost host;
    private readonly ILogger? logger;

    public Router(AgentHost host, ILogger? logger)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.logger = logger;
    }

    public async Task Respond(IAgent agent, TP3Message message, INetworkPipe targetTransport)
    {
        // someone asked about this while ago, and i can respond to it but where :-)
        await targetTransport.TP3Transport.Send(targetTransport, message);
    }

    public async Task Route(INetworkPipe sourceTransport, TP3Message message)
    {
        logger?.LogDebug("Routing TP3 message: {Command}", message.Command);

#warning TODO: namespace filtering
#warning TODO: tcp forward, currelnty we only send to our local agent, but we should forward to other agents if the target is not local

        await host.Me.Handle(sourceTransport, message);
    }
}
