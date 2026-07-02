using TP3.Messages;

namespace TP3.Messages;

public sealed class TP3GenericMessage : TP3Message
{
    public TP3GenericMessage()
        : base(TP3Command.READ)
    {
    }

    public TP3GenericMessage(TP3Command command)
        : base(command)
    {
    }
}
