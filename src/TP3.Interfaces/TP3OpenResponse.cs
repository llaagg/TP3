namespace TP3.Messages;

public class TP3OpenResponse : TP3Message
{
    public TP3OpenResponse(string tag, NodeInfo nodeInfo, uint iounit)
        : base(TP3Command.OPEN)
    {
        Tag = tag;
        Info = nodeInfo;
        Iounit = iounit;
    }

    public NodeInfo Info { get; }

    /// <summary>
    /// Suggested amount of bytes to read per chunk. Usually service knows and maps to most reasonable size
    /// for ex.: disk allocation block size, or network MTU size, etc.
    /// frame for sound or video streaming, etc.
    /// if 0 then no limit, read as much as possible.
    /// </summary>
    public uint Iounit { get; } = 0;
}