using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public sealed class TP3WalkResponse : TP3Message
{
    public TP3WalkResponse()
        : base(TP3Command.WALK)
    {
    }

    [ProtoMember(1)]
    public List<NodeInfo> Infos { get; set; } = new List<NodeInfo>();
}
