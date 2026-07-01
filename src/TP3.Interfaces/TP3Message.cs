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

    public override string ToString()
    {
        if (Command == TP3Command.NONE)
        {
            return string.Empty;
        }

        if (Path.Count == 0)
        {
            return Payload == null
                ? Command.ToString()
                : $"{Command} {Payload}";
        }

        return Payload == null
            ? $"{Command} {string.Join("/", Path)}"
            : $"{Command} {string.Join("/", Path)} {Payload}";
    }
}
