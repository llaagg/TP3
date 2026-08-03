using TP3.Interfaces;

namespace TP3.Protocol.Base;

public abstract class RWStream : ITP3DataStream
{
    public uint Iounit => 0;

    public ulong Position { get; protected set; } = 0;

    public void Close()
    {
    }

    public async Task Open()
    {
        
    }

    public async Task<byte[]> Read(ulong offset, ulong maxCount)
    {
        return await OnRead(offset, maxCount);
    }

    public async Task<ulong> Write(ulong offset, byte[] data)
    {
        var incomingData = data;
        await OnWrite(incomingData);
        return (ulong)data.Length;
    }

    public virtual async Task OnWrite(byte[] data)
    {
        
    }

    public virtual async Task<byte[]> OnRead(ulong offset, ulong maxCount)
    {
        return new byte[0];
    }
}
