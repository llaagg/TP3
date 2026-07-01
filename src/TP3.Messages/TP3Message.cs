namespace TP3.Messages;


public class TP3Message
{
    public TP3Message()
    {
    }
    

    /// <summary>
    /// Initializes a new instance of the <see cref="TP3Message"/> class with the specified command, target, and payload.
    /// </summary>
    public TP3Message(TP3Command command, string target, string payload)
    {
        Command = command;
        Target = target;
        Payload = payload;
    }

    /// <summary>
    /// Copy constructor for TP3Message.
    /// </summary>
    public TP3Message(TP3Message other) : this(other.Command, other.Target, other.Payload)
    {
    }
    
    public TP3Command Command { get; init; } = TP3Command.NONE;
    public string Target { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;

    public bool IsEmpty => Command == TP3Command.NONE && string.IsNullOrWhiteSpace(Target) && string.IsNullOrWhiteSpace(Payload);

    public override string ToString()
    {
        if (Command == TP3Command.NONE)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(Target))
        {
            return string.IsNullOrWhiteSpace(Payload)
                ? Command.ToString()
                : $"{Command} {Payload}";
        }

        return string.IsNullOrWhiteSpace(Payload)
            ? $"{Command} {Target}"
            : $"{Command} {Target} {Payload}";
    }
}
