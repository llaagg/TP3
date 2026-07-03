using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public class TP3OpenRequest : TP3Message
{
    public TP3OpenRequest(string tag)
        : base(TP3Command.OPEN)
    {
        Tag = tag;
    }
}
