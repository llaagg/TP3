namespace TP3.Messages;

public sealed class TP3WalkRequest : TP3Message
{
    private List<string> _args = new();

    public TP3WalkRequest()
    {
        Command = TP3Command.WALK;
    }

    // Walk request intentionally carries only path args and correlation tag.
    public new List<string> Args
    {
        get => _args;
        init => _args = value ?? new List<string>();
    }

    public new string? Tag { get; init; }

    public static TP3WalkRequest From(TP3Message message)
    {
        if (message.Command != TP3Command.WALK)
        {
            throw new ArgumentException("Expected WALK command.", nameof(message));
        }

        return new TP3WalkRequest
        {
            Args = message.Args,
            Tag = message.Tag
        };
    }
}
