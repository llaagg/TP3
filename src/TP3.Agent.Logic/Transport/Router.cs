using System;
using Microsoft.Extensions.Logging;

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

        var parts = request.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToUpperInvariant();
        var payload = parts.Length > 1 ? parts[1] : string.Empty;

        return command switch
        {
            "GET" => RouteGet(payload),
            "PUBLISH" => RoutePublish(payload),
            "NAMESPACE" => RouteNamespace(payload),
            _ => RouteAgentRequest(request)
        };
    }

    private string RouteGet(string payload)
    {
        return payload.ToUpperInvariant() switch
        {
            "META" => string.Join("; ", host.Me.MetaData.Select(item => $"{item.Name}={item.Value}")),
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

    private string RouteNamespace(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "MISSING NAMESPACE COMMAND";
        }

        var parts = payload.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var action = parts[0].ToUpperInvariant();
        var argument = parts.Length > 1 ? parts[1] : string.Empty;

        return action switch
        {
            "CREATE" when !string.IsNullOrWhiteSpace(argument) => host.CreateNamespace(argument).Name,
            "GET" when !string.IsNullOrWhiteSpace(argument) => host.GetNamespace(argument) != null ? "NAMESPACE FOUND" : "NAMESPACE NOT FOUND",
            "LIST" => string.Join(", ", host.Namespaces.Select(ns => ns.Name)),
            _ => "UNKNOWN NAMESPACE COMMAND"
        };
    }

    private string RouteAgentRequest(string request)
    {
        return host.Me.HandleRequest(request);
    }
}
