namespace TP3.Interfaces;

public interface ITP3DataStream
{
    public uint Iounit { get; }
    Task Open();
    Task<byte[]> Read(ulong offset, ulong maxCount);
    void Close();
    ulong Position { get; }
}
