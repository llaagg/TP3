using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public interface ITP3Serializer
{
    byte[] Header { get; }
    TP3SerializationFormat Format { get; }
    byte[] SerializePayload(TP3Message message);
    TP3Message DeserializePayload(ReadOnlySpan<byte> payload);
}
