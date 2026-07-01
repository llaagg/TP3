using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public static class TP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly byte[] Header = Utf8.GetBytes("TP3");

    public static byte[] SerializeBytes(TP3Message message)
    {
        var payload = SerializePayload(message);
        var packet = new byte[Header.Length + sizeof(int) + payload.Length];

        Buffer.BlockCopy(Header, 0, packet, 0, Header.Length);
        var lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
        Buffer.BlockCopy(lengthBytes, 0, packet, Header.Length, lengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, Header.Length + lengthBytes.Length, payload.Length);

        return packet;
    }

    public static string SerializeText(TP3Message message)
        => message.ToString();

    public static TP3Message DeserializeBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= Header.Length + sizeof(int)
            && bytes[0] == Header[0]
            && bytes[1] == Header[1]
            && bytes[2] == Header[2])
        {
            return DeserializePacket(bytes);
        }

        return Deserialize(Utf8.GetString(bytes));
    }

    public static async Task<TP3Message> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[Header.Length];
        await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);

        if (header[0] != Header[0] || header[1] != Header[1] || header[2] != Header[2])
        {
            throw new InvalidDataException("Invalid TP3 header.");
        }

        var lengthBytes = new byte[sizeof(int)];
        await ReadExactAsync(stream, lengthBytes, cancellationToken).ConfigureAwait(false);
        var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

        if (payloadLength < 0)
        {
            throw new InvalidDataException("Invalid TP3 payload length.");
        }

        var payload = new byte[payloadLength];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return DeserializePayload(payload);
    }

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

    private static byte[] SerializePayload(TP3Message message)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Utf8, leaveOpen: true);

        writer.Write((int)message.Command);
        writer.Write(message.Target ?? string.Empty);
        writer.Write(message.Payload ?? string.Empty);
        writer.Flush();

        return memoryStream.ToArray();
    }

    private static TP3Message DeserializePacket(ReadOnlySpan<byte> bytes)
    {
        var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes.Slice(Header.Length, sizeof(int)).ToArray(), 0));
        return DeserializePayload(bytes.Slice(Header.Length + sizeof(int), payloadLength));
    }

    private static TP3Message DeserializePayload(ReadOnlySpan<byte> payload)
    {
        using var memoryStream = new MemoryStream(payload.ToArray());
        using var reader = new BinaryReader(memoryStream, Utf8, leaveOpen: true);

        var commandValue = reader.ReadInt32();
        var command = Enum.IsDefined(typeof(TP3Command), commandValue)
            ? (TP3Command)commandValue
            : TP3Command.ECHO;

        var target = reader.ReadString();
        var messagePayload = reader.ReadString();

        return new TP3Message
        {
            Command = command,
            Target = target,
            Payload = messagePayload
        };
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;

        while (offset < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("Unexpected EOF while reading TP3 packet.");
            }

            offset += bytesRead;
        }
    }
}
