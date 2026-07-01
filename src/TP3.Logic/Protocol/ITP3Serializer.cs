using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

public interface ITP3Serializer
{
    byte[] Header { get; }
    byte[] SerializePayload(TP3Message message);
    TP3Message DeserializePayload(ReadOnlySpan<byte> payload);
}
