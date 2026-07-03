using System.Text;
using TP3.Interfaces;
using TP3.Messages;

namespace TP3.Agent.Logic.Protocol;

internal sealed class BinaryTP3Serializer : ITP3Serializer
{
    private static readonly Encoding Utf8 = Encoding.UTF8;
    public byte[] Header { get; } = Utf8.GetBytes("TP30");

    public byte[] SerializePayload(TP3Message message)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Utf8, leaveOpen: true);

        writer.Write((int)message.Command);
        writer.Write(message is TP3WalkResponse or TP3ReadResponse ? (byte)1 : (byte)0);
        writer.Write(message.Tag ?? string.Empty);

        switch (message)
        {
            case TP3WalkResponse walkResponse:
                break;

            case TP3ReadRequest readRequest:
                writer.Write(readRequest.Offset);
                writer.Write(readRequest.MaxBytes);
                break;

            case TP3ReadResponse readResponse:
                var data = readResponse.Data ?? Array.Empty<byte>();
                writer.Write(data.Length);
                writer.Write(data);
                break;

            case TP3WalkRequest:
            case TP3GenericMessage:
            default:
                break;
        }

        return memoryStream.ToArray();
    }

    public TP3Message DeserializePayload(ReadOnlySpan<byte> payload)
    {
        using var memoryStream = new MemoryStream(payload.ToArray());
        using var reader = new BinaryReader(memoryStream, Utf8, leaveOpen: true);

        var commandValue = reader.ReadInt32();
        if (!Enum.TryParse<TP3Command>(commandValue.ToString(), out var command))
        {
            throw new InvalidDataException($"Invalid TP3 command value: {commandValue}");
        }

        var kind = reader.ReadByte();
        var args = ReadStringList(reader);
        var tag = reader.ReadString();

        var isResponse = kind == 1;
        return command switch
        {
            TP3Command.WALK => isResponse ? ReadWalkResponse(args, tag, reader) : new TP3WalkRequest(NormalizeOptionalString(tag))
            {
                Path = args,
            },
            TP3Command.READ => isResponse ? ReadReadResponse(args, tag, reader) : new TP3ReadRequest
            {
                Tag = NormalizeOptionalString(tag),
                Offset = (ulong)reader.ReadInt64(),
                MaxBytes = (uint)reader.ReadInt32()
            },
            _ => new TP3GenericMessage(command)
            {
                Tag = NormalizeOptionalString(tag)
            }
        };
    }

    private static TP3WalkResponse ReadWalkResponse(List<string> args, string tag, BinaryReader reader)
    {
        return new TP3WalkResponse
        {
            Tag = NormalizeOptionalString(tag),
        };
    }

    private static TP3ReadResponse ReadReadResponse(List<string> args, string tag, BinaryReader reader)
    {
        var dataLength = reader.ReadInt32();
        var data = dataLength > 0 ? reader.ReadBytes(dataLength) : Array.Empty<byte>();
        var error = NormalizeOptionalString(reader.ReadString());

        return new TP3ReadResponse
        {
            Tag = NormalizeOptionalString(tag),
            Data = dataLength > 0 ? data : null,
        };
    }

    private static void WriteStringList(BinaryWriter writer, IReadOnlyList<string> values)
    {
        writer.Write(values.Count);
        foreach (var value in values)
        {
            writer.Write(value ?? string.Empty);
        }
    }

    private static List<string> ReadStringList(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        var list = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(reader.ReadString());
        }

        return list;
    }

    private static NodeType? TryReadNodeType(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && Enum.TryParse<NodeType>(value, ignoreCase: true, out var nodeType)
            ? nodeType
            : null;
    }

    private static string? NormalizeOptionalString(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
