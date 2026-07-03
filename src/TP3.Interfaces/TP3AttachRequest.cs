using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public class TP3AttachRequest : TP3Message
{
    public TP3AttachRequest(string tag)
        : base(TP3Command.ATTACH)
    {
        Tag = tag;
    }
}
