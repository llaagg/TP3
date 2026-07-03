using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public sealed class TP3ReadRequest : TP3Message
{
    public TP3ReadRequest(string? tag = null, ulong offset = 0, uint maxBytes = 0)
        : base(TP3Command.READ)
    {
        if (tag != null)
        {
            Tag = tag;
        }
        Offset = offset;
        MaxBytes = maxBytes;
    }

    [ProtoMember(1)]
    public ulong Offset { get; init; }

    [ProtoMember(2)]
    public uint MaxBytes { get; init; }
}

