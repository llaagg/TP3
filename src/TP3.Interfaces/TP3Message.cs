using TP3.Interfaces;

namespace TP3.Messages;

public class TP3Message
{
    public TP3Message()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TP3Message"/> class with the specified command and path.
    /// </summary>
    public TP3Message(TP3Command command, params string[] path)
    {
        Command = command;
        Path = path.ToList();
    }

    /// <summary>
    /// Copy constructor for TP3Message.
    /// </summary>
    public TP3Message(TP3Message other) : this(other.Command, other.Path.ToArray())
    {
    }
    
    public TP3Command Command { get; init; } = TP3Command.NONE;

    /// <summary>
    /// Which node you are referring to
    /// </summary>
    public List<string> Path { get; init; } = new List<string>();
    
    public ITP3Stream? Payload { get; init; }

    public string? Qid { get; init; }

    public string? NodeType { get; init; }

    public bool IsChunk { get; init; }

    public int ChunkIndex { get; init; }

    public bool IsFinalChunk { get; init; }

    public byte[]? Data { get; init; }

    public string? Error { get; init; }

    public override string ToString()
    {
        if (Command == TP3Command.NONE)
        {
            return string.Empty;
        }

        var suffix = string.Empty;
        if (IsChunk)
        {
            var dataLength = Data?.Length ?? 0;
            suffix = $" chunk={ChunkIndex} final={IsFinalChunk} bytes={dataLength}";
        }

        if (!string.IsNullOrWhiteSpace(Qid))
        {
            suffix += $" qid={Qid}";
        }

        if (!string.IsNullOrWhiteSpace(Error))
        {
            suffix += $" error={Error}";
        }

        if (Path.Count == 0)
        {
            return Payload == null
                ? $"{Command}{suffix}"
                : $"{Command} {Payload}{suffix}";
        }

        return Payload == null
            ? $"{Command} {string.Join("/", Path)}{suffix}"
            : $"{Command} {string.Join("/", Path)} {Payload}{suffix}";
    }
}
