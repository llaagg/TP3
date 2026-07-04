using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public static class TP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    private static readonly int HeaderLength = 4;
    private static readonly ITP3Serializer BinarySerializer = new BinaryTP3Serializer();

    public static byte[] SerializeBytes(TP3Message message)
    {
        var payload = BinarySerializer.SerializePayload(message);
        return BuildPacket(BinarySerializer.Header, payload);
    }

    public static async Task<TP3Message> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var header = new byte[HeaderLength];
        await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);

        var lengthBytes = new byte[sizeof(int)];
        await ReadExactAsync(stream, lengthBytes, cancellationToken).ConfigureAwait(false);
        var payloadLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

        if (payloadLength < 0)
        {
            throw new InvalidDataException("Invalid TP3 payload length.");
        }

        var payload = new byte[payloadLength];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return BinarySerializer.DeserializePayload(payload);
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
