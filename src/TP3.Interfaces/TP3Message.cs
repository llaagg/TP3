using TP3.Interfaces;

namespace TP3.Messages;

public class TP3Message
{
    private List<string> _args = new();

    public TP3Message()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TP3Message"/> class with the specified command and arguments.
    /// </summary>
    public TP3Message(TP3Command command, params string[] args)
    {
        Command = command;
        _args = args.ToList();
    }

    /// <summary>
    /// Copy constructor for TP3Message.
    /// </summary>
    public TP3Message(TP3Message other) : this(other.Command, other.Args.ToArray())
    {
    }
    
    public TP3Command Command { get; init; } = TP3Command.WALK;

    /// <summary>
    /// The arguments for the TP3 message.
    /// </summary>
    public List<string> Args
    {
        get => _args;
        init => _args = value ?? new List<string>();
    }

    public string? Qid { get; init; }

    public string? NodeType { get; init; }

    public bool IsChunk { get; init; }

    public int ChunkIndex { get; init; }

    public bool IsFinalChunk { get; init; }

    public byte[]? Data { get; init; }

    public string? Error { get; init; }

    public override string ToString()
    {
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

        return $"TP3Message cmd={Command} args={string.Join('/', Args)}{suffix}";
    }
}
