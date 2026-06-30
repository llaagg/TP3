using System;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Host;
using TP3.Agent.Logic.Protocol;

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

    public string Route(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var request = message.Trim();
        logger?.LogDebug("Routing message: {Message}", request);

        var tp3Message = TP3Protocol.Parse(request);
        return Route(tp3Message);
    }

    public string Route(TP3Message message)
    {
        if (message.IsEmpty)
        {
            logger?.LogWarning("Received empty TP3 message.");
            return string.Empty;
        }

        logger?.LogDebug("Routing TP3 message: {Command} {Target}", message.Command, message.Target);

        return RouteAgentRequest(message);
    }


    private string RouteAgentRequest(TP3Message message)
    {
        #warning TODO: namespace filtering
        #warning TODO: tcp forward
        var request = string.IsNullOrWhiteSpace(message.Target)
            ? message.Payload
            : $"{message.Target} {message.Payload}".Trim();

        return host.Me.HandleRequest(request);
    }
}
