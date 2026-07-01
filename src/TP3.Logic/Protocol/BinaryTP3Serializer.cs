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
        writer.Write(message.Args.Count);
        foreach (var segment in message.Args)
        {
            writer.Write(segment ?? string.Empty);
        }

        writer.Write(message.Tag ?? string.Empty);
        writer.Write(message.Qid ?? string.Empty);
        writer.Write(message.Offset);
        writer.Write(message.MaxBytes);
        writer.Write(message.NodeType?.ToString().ToLowerInvariant() ?? string.Empty);
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

        if(!Enum.TryParse<TP3Command>(commandValue.ToString(), out var command))
        {
            throw new InvalidDataException($"Invalid TP3 command value: {commandValue}");
        }

        var pathCount = reader.ReadInt32();
        var path = new List<string>(pathCount);
        for (var i = 0; i < pathCount; i++)
        {
            path.Add(reader.ReadString());
        }

        var tag = reader.ReadString();
        var qid = reader.ReadString();
        var offset = reader.ReadInt64();
        var maxBytes = reader.ReadInt32();
        var nodeTypeRaw = reader.ReadString();
        NodeType? nodeType = null;
        if (!string.IsNullOrWhiteSpace(nodeTypeRaw)
            && Enum.TryParse<NodeType>(nodeTypeRaw, ignoreCase: true, out var parsedNodeType))
        {
            nodeType = parsedNodeType;
        }
        var isChunk = reader.ReadBoolean();
        var chunkIndex = reader.ReadInt32();
        var isFinalChunk = reader.ReadBoolean();
        var dataLength = reader.ReadInt32();
        var data = dataLength > 0 ? reader.ReadBytes(dataLength) : Array.Empty<byte>();
        var error = reader.ReadString();

        return new TP3Message
        {
            Command = command,
            Args = path,
            Tag = string.IsNullOrWhiteSpace(tag) ? null : tag,
            Qid = string.IsNullOrWhiteSpace(qid) ? null : qid,
            Offset = offset,
            MaxBytes = maxBytes,
            NodeType = nodeType,
            IsChunk = isChunk,
            ChunkIndex = chunkIndex,
            IsFinalChunk = isFinalChunk,
            Data = dataLength > 0 ? data : null,
            Error = string.IsNullOrWhiteSpace(error) ? null : error
        };
    }
}
