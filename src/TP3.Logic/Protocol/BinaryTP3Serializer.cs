using System.Text;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

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
        writer.Write(message.Path.Count);
        foreach (var segment in message.Path)
        {
            writer.Write(segment ?? string.Empty);
        }

        writer.Write(message.Qid ?? string.Empty);
        writer.Write(message.NodeType ?? string.Empty);
        writer.Write(message.IsChunk);
        writer.Write(message.ChunkIndex);
        writer.Write(message.IsFinalChunk);

        var data = message.Data ?? Array.Empty<byte>();
        writer.Write(data.Length);
        writer.Write(data);

        writer.Write(message.Error ?? string.Empty);
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

        var pathCount = reader.ReadInt32();
        var path = new List<string>(pathCount);
        for (var i = 0; i < pathCount; i++)
        {
            path.Add(reader.ReadString());
        }

        var qid = reader.ReadString();
        var nodeType = reader.ReadString();
        var isChunk = reader.ReadBoolean();
        var chunkIndex = reader.ReadInt32();
        var isFinalChunk = reader.ReadBoolean();
        var dataLength = reader.ReadInt32();
        var data = dataLength > 0 ? reader.ReadBytes(dataLength) : Array.Empty<byte>();
        var error = reader.ReadString();

        return new TP3Message
        {
            Command = command,
            Path = path,
            Qid = string.IsNullOrWhiteSpace(qid) ? null : qid,
            NodeType = string.IsNullOrWhiteSpace(nodeType) ? null : nodeType,
            IsChunk = isChunk,
            ChunkIndex = chunkIndex,
            IsFinalChunk = isFinalChunk,
            Data = dataLength > 0 ? data : null,
            Error = string.IsNullOrWhiteSpace(error) ? null : error
        };
    }
}
