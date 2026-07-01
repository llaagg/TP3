using TP3.Messages;

namespace TP3.Messages;

public sealed class TP3GenericMessage : TP3Message
{
    public TP3GenericMessage()
    {
    }

    public TP3GenericMessage(TP3Command command, params string[] args)
        : base(command, args)
    {
    }
}
