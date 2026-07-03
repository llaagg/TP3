using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
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