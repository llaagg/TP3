namespace TP3.Interfaces;

public interface ITP3DataStream
{
    public uint Iounit { get; }
    Task Open();
    Task<byte[]> Read(ulong offset, ulong maxCount);
    Task<ulong> Write(ulong offset, byte[] data);
    void Close();
    ulong Position { get; }
}
