using System;
using System.Text;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public static class TP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;

    public static byte[] SerializeBytes(TP3Message message)
    {
        var text = message.ToString();
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<byte>();
        }

        return Utf8.GetBytes(text + "\n");
    }

    public static string SerializeText(TP3Message message)
        => message.ToString();

    public static TP3Message DeserializeBytes(ReadOnlySpan<byte> bytes)
        => Deserialize(Utf8.GetString(bytes));

    public static TP3Message Deserialize(string request)
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

        var command = Enum.TryParse<TP3Command>(parts[0], true, out var parsedCommand)
            ? parsedCommand
            : TP3Command.ECHO;

        var target = parts.Length > 1 ? parts[1] : string.Empty;
        var payload = parts.Length > 2 ? parts[2] : string.Empty;

        return new TP3Message
        {
            Command = command,
            Target = target,
            Payload = payload
        };
    }
}
