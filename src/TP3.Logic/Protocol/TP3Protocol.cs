using System;
using System.Linq;

namespace TP3.Agent.Logic.Protocol;

public static class TP3Protocol
{
    public static TP3Message Parse(string request)
    {
        if (string.IsNullOrWhiteSpace(request))
        {
            return new TP3Message();
        }

        var parts = request.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return new TP3Message();
        }

        var command = parts[0].ToUpperInvariant();
        var target = parts.Length > 1 ? parts[1] : string.Empty;
        var payload = parts.Length > 2 ? parts[2] : string.Empty;

        return new TP3Message
        {
            Command = command,
            Target = target,
            Payload = payload
        };
    }

    public static bool IsStreamHandshake(string request)
        => string.Equals(request?.Trim(), "STREAM", StringComparison.OrdinalIgnoreCase);

    public static string FormatResponse(string response)
        => string.IsNullOrEmpty(response) ? string.Empty : response;
}
