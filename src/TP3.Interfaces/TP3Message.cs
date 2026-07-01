using TP3.Interfaces;

namespace TP3.Messages;

public abstract class TP3Message
{
    private List<string> _args = new();

    protected TP3Message()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TP3Message"/> class with the specified command and arguments.
    /// </summary>
    protected TP3Message(TP3Command command, params string[] args)
    {
        Command = command;
        _args = args.ToList();
    }

    /// <summary>
    /// Copy constructor for TP3Message.
    /// </summary>
    protected TP3Message(TP3Message other) : this(other.Command, other.Args.ToArray())
    {
        Tag = other.Tag;
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

    public string? Tag { get; init; }

    public override string ToString()
    {
        return $"TP3Message cmd={Command} args={string.Join('/', Args)}";
    }
}
