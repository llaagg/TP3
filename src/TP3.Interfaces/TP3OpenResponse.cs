using ProtoBuf;

namespace TP3.Messages;

[ProtoContract]
public class TP3OpenResponse : TP3Message
{
    public TP3OpenResponse(string tag, NodeInfo nodeInfo, uint iounit)
        : base(TP3Command.OPEN)
    {
        Tag = tag;
        Info = nodeInfo;
        Iounit = iounit;
    }

    [ProtoMember(1)]
    public NodeInfo Info { get; }

    /// <summary>
    /// Suggested amount of bytes to read per chunk. Usually service knows and maps to most reasonable size
    /// for ex.: disk allocation block size, or network MTU size, etc.
    /// frame for sound or video streaming, etc.
    /// if 0 then no limit, read as much as possible.
    /// </summary>
    [ProtoMember(2)]
    public uint Iounit { get; } = 0;
}