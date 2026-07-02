namespace TP3.Messages;

public class TP3OpenResponse : TP3Message
{
    public TP3OpenResponse(string tag, NodeInfo nodeInfo, uint iounit)
        : base(TP3Command.OPEN)
    {
        Tag = tag;
        Qid = nodeInfo;
        Iounit = iounit;
    }

    public NodeInfo Qid { get; }

    /// <summary>
    /// Suggeted amount of bytes to read perchunk. Usually service knows and maps to most reasoble size
    /// for ex.: disk allocation block size, or network MTU size, etc.
    /// frame for sound or video streaming, etc.
    /// if 0 then no limit, read as much as possible.
    /// </summary>
    public uint Iounit { get; } = 0;
}