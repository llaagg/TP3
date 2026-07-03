using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public class TP3ClunkRequest : TP3Message
{
    public TP3ClunkRequest(string tag)
        : base(TP3Command.CLUNK)
    {
        Tag = tag;
    }
}

[ProtoContract]
public class TP3ClunkResponse : TP3Message
{
    public TP3ClunkResponse(string tag)
        : base(TP3Command.CLUNK)
    {
        Tag = tag;
    }
}
