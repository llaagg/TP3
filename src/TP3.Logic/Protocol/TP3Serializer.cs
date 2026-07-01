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

public static class TP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly byte[] BinaryHeader = Utf8.GetBytes("TP30");
    private static readonly byte[] JsonHeader = Utf8.GetBytes("TP31");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static byte[] SerializeBytes(TP3Message message, TP3SerializationFormat format = TP3SerializationFormat.Binary)
    {
        var header = format == TP3SerializationFormat.Json ? JsonHeader : BinaryHeader;
        var payload = format == TP3SerializationFormat.Json
            ? SerializeJsonPayload(message)
            : SerializeBinaryPayload(message);

        var packet = new byte[header.Length + sizeof(int) + payload.Length];
        Buffer.BlockCopy(header, 0, packet, 0, header.Length);
        var lengthBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
        Buffer.BlockCopy(lengthBytes, 0, packet, header.Length, lengthBytes.Length);
        Buffer.BlockCopy(payload, 0, packet, header.Length + lengthBytes.Length, payload.Length);

        return packet;
    }

    public static TP3Message DeserializeBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < BinaryHeader.Length + sizeof(int))
        {
            throw new InvalidDataException("Invalid TP3 packet.");
        }

        var header = bytes.Slice(0, BinaryHeader.Length);
        var format = GetFormat(header);
        var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes.Slice(BinaryHeader.Length, sizeof(int)).ToArray(), 0));
        var payload = bytes.Slice(BinaryHeader.Length + sizeof(int), payloadLength);

        return DeserializePayload(payload, format);
    }

    public static async Task<TP3Message> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[BinaryHeader.Length];
        await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);

        var format = GetFormat(header);
        var lengthBytes = new byte[sizeof(int)];
        await ReadExactAsync(stream, lengthBytes, cancellationToken).ConfigureAwait(false);
        var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

        if (payloadLength < 0)
        {
            throw new InvalidDataException("Invalid TP3 payload length.");
        }

        var payload = new byte[payloadLength];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return DeserializePayload(payload, format);
    }

    private static byte[] SerializeBinaryPayload(TP3Message message)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Utf8, leaveOpen: true);

        writer.Write((int)message.Command);
        writer.Write(message.Target ?? string.Empty);
        writer.Write(message.Payload ?? string.Empty);
        writer.Flush();

        return memoryStream.ToArray();
    }

    private static byte[] SerializeJsonPayload(TP3Message message)
    {
        return Utf8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
    }

    private static TP3Message DeserializePayload(ReadOnlySpan<byte> payload, TP3SerializationFormat format)
    {
        return format == TP3SerializationFormat.Json
            ? DeserializeJsonPayload(payload)
            : DeserializeBinaryPayload(payload);
    }

    private static TP3Message DeserializeBinaryPayload(ReadOnlySpan<byte> payload)
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

    private static TP3Message DeserializeJsonPayload(ReadOnlySpan<byte> payload)
    {
        var json = Utf8.GetString(payload);
        return JsonSerializer.Deserialize<TP3Message>(json, JsonOptions)
            ?? new TP3Message();
    }

    private static TP3SerializationFormat GetFormat(ReadOnlySpan<byte> header)
    {
        if (header.SequenceEqual(BinaryHeader))
        {
            return TP3SerializationFormat.Binary;
        }

        if (header.SequenceEqual(JsonHeader))
        {
            return TP3SerializationFormat.Json;
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
