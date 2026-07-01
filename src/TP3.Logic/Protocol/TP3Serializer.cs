using System;
using System.IO;
using System.Net;
using System.Text;
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
    private static readonly int HeaderLength = 4;
    private static readonly ITP3Serializer BinarySerializer = new BinaryTP3Serializer();
    private static readonly ITP3Serializer JsonSerializer = new JsonTP3Serializer();

    public static byte[] SerializeBytes(TP3Message message, TP3SerializationFormat format = TP3SerializationFormat.Json)
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
                throw new EndOfStreamException("Unexpected EOF while reading TP3 packet.");
            }

            offset += bytesRead;
        }
    }
}
