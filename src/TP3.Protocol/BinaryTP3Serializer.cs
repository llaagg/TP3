using System.IO;
using Google.Protobuf;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

internal sealed class BinaryTP3Serializer : ITP3Serializer
{
    public byte[] Header { get; } = new byte[] { (byte)'T', (byte)'P', (byte)'3', (byte)'0' };

    public byte[] SerializePayload(TP3Message message)
    {
        return message.ToByteArray();
    }

    public TP3Message DeserializePayload(ReadOnlySpan<byte> payload)
    {
        return TP3Message.Parser.ParseFrom(payload.ToArray());
    }
}
