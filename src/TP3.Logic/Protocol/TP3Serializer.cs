using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public enum TP3SerializationFormat
{
    Binary,
    Json
}

public interface ITP3Serializer
{
    byte[] Header { get; }
    TP3SerializationFormat Format { get; }
    byte[] SerializePayload(TP3Message message);
    TP3Message DeserializePayload(ReadOnlySpan<byte> payload);
}

public static class TP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly int HeaderLength = 4;
    private static readonly ITP3Serializer BinarySerializer = new BinaryTP3Serializer();
    private static readonly ITP3Serializer JsonSerializer = new JsonTP3Serializer();

    public static byte[] SerializeBytes(TP3Message message, TP3SerializationFormat format = TP3SerializationFormat.Binary)
    {
        var serializer = GetSerializer(format);
        var payload = serializer.SerializePayload(message);
        return BuildPacket(serializer.Header, payload);
    }

    public static TP3Message DeserializeBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderLength + sizeof(int))
        {
            throw new InvalidDataException("Invalid TP3 packet.");
        }

        var header = bytes.Slice(0, HeaderLength);
        var serializer = GetSerializer(header);
        var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes.Slice(HeaderLength, sizeof(int)).ToArray(), 0));
        var payload = bytes.Slice(HeaderLength + sizeof(int), payloadLength);

        return serializer.DeserializePayload(payload);
    }

    public static async Task<TP3Message> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderLength];
        await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);

        var serializer = GetSerializer(header);
        var lengthBytes = new byte[sizeof(int)];
        await ReadExactAsync(stream, lengthBytes, cancellationToken).ConfigureAwait(false);
        var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

        if (payloadLength < 0)
        {
            throw new InvalidDataException("Invalid TP3 payload length.");
        }

        var payload = new byte[payloadLength];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return serializer.DeserializePayload(payload);
    }

    private static byte[] BuildPacket(byte[] header, byte[] payload)
    {
        var packet = new byte[header.Length + sizeof(int) + payload.Length];
        Buffer.BlockCopy(header, 0, packet, 0, header.Length);
        var lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
        Buffer.BlockCopy(lengthBytes, 0, packet, header.Length, lengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, header.Length + lengthBytes.Length, payload.Length);
        return packet;
    }

    private static ITP3Serializer GetSerializer(TP3SerializationFormat format)
        => format == TP3SerializationFormat.Json ? JsonSerializer : BinarySerializer;

    private static ITP3Serializer GetSerializer(ReadOnlySpan<byte> header)
    {
        if (header.SequenceEqual(BinarySerializer.Header))
        {
            return BinarySerializer;
        }

        if (header.SequenceEqual(JsonSerializer.Header))
        {
            return JsonSerializer;
        }

        throw new InvalidDataException("Unknown TP3 header.");
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

internal sealed class BinaryTP3Serializer : ITP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    public byte[] Header { get; } = Utf8.GetBytes("TP30");
    public TP3SerializationFormat Format => TP3SerializationFormat.Binary;

    public byte[] SerializePayload(TP3Message message)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Utf8, leaveOpen: true);

        writer.Write((int)message.Command);
        writer.Write(message.Target ?? string.Empty);
        writer.Write(message.Payload ?? string.Empty);
        writer.Flush();

        return memoryStream.ToArray();
    }

    public TP3Message DeserializePayload(ReadOnlySpan<byte> payload)
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
}

internal sealed class JsonTP3Serializer : ITP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public byte[] Header { get; } = Utf8.GetBytes("TP31");
    public TP3SerializationFormat Format => TP3SerializationFormat.Json;

    public byte[] SerializePayload(TP3Message message)
        => Utf8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));

    public TP3Message DeserializePayload(ReadOnlySpan<byte> payload)
    {
        var json = Utf8.GetString(payload);
        return JsonSerializer.Deserialize<TP3Message>(json, JsonOptions)
            ?? new TP3Message();
    }
}
