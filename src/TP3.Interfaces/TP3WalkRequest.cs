namespace TP3.Messages;

public sealed class TP3WalkRequest : TP3Message
{
    public TP3WalkRequest()
    {
        Command = TP3Command.WALK;
    }

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
