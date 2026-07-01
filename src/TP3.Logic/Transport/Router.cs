using System;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Protocol;
using TP3.Messages;

namespace TP3.Agent.Logic.Transport;

public sealed class Router
{
    private readonly AgentHost host;
    private readonly ILogger? logger;

    public Router(AgentHost host, ILogger? logger)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.logger = logger;
    }

    public async Task Route(TP3Message message)
    {
        if (message.Command == TP3Command.NONE)
        {
            logger?.LogWarning($"Received {message.Command} TP3 message.");
            return;
        }

        logger?.LogDebug("Routing TP3 message: {Command} {Target}", message.Command, message.Target);

        #warning TODO: namespace filtering
        #warning TODO: tcp forward, currelnty we only send to our local agent, but we should forward to other agents if the target is not local
        
        await host.Me.Handle(message);
    }
}
