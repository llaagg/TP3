using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public sealed class TP3WalkRequest : TP3Message
{
    public TP3WalkRequest(
        string tag,
        string newTag = null!,
        List<string>? path = null)
        : base(TP3Command.WALK)
    {
        Path = path ?? new List<string>();
        Tag = tag;
        NewTag = newTag;
    }

    [ProtoMember(1)]
    public List<string> Path { get; set; } = new List<string>();

    [ProtoMember(2)]
    public string? NewTag { get; set; } = null!;
}
