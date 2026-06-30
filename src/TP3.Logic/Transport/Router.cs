using System;
using Microsoft.Extensions.Logging;
using TP3.Agent.Logic.Protocol;

namespace TP3.Agent.Logic.Host;

public sealed class Router
{
    private readonly AgentHost host;
    private readonly ILogger? logger;

    public Router(AgentHost host, ILogger? logger = null)
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
            return string.Empty;
        }

        logger?.LogDebug("Routing TP3 message: {Command} {Target}", message.Command, message.Target);

        return message.Command switch
        {
            "GET" => RouteGet(message.Target),
            "PUBLISH" => RoutePublish(message.Payload),
            _ => RouteAgentRequest(message)
        };
    }

    private string RouteGet(string payload)
    {
        return payload.ToUpperInvariant() switch
        {
            "TRUNK" => host.Me.T?.ToString() ?? "No trunk available",
            _ => $"UNKNOWN GET TARGET: {payload}"
        };
    }

    private string RoutePublish(string payload)
    {
        if (payload.StartsWith("EVENT ", StringComparison.OrdinalIgnoreCase))
        {
            var eventText = payload[6..].Trim();
            host.PublishEvent(eventText);
            return "EVENT PUBLISHED";
        }

        return "UNKNOWN PUBLISH COMMAND";
    }

    private string RouteAgentRequest(TP3Message message)
    {
        var request = string.IsNullOrWhiteSpace(message.Target)
            ? message.Payload
            : $"{message.Target} {message.Payload}".Trim();

        return host.Me.HandleRequest(request);
    }
}
