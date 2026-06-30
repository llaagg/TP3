using System;
using System.Linq;
using TP3.Messages;

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

        var command = Enum.TryParse<TP3Command>(parts[0], true, out var parsedCommand) ? parsedCommand : TP3Command.ECHO;
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
