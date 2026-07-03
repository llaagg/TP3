using TP3.Messages;

namespace TP3.Interfaces;

public interface ITP3Serializer
{
    byte[] Header { get; }
    byte[] SerializePayload(TP3Message message);
    TP3Message DeserializePayload(ReadOnlySpan<byte> payload);
}
