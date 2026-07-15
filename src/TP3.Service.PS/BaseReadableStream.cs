using TP3.Interfaces;

public abstract class BaseReadableStream : Stream
{
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => 0;

    public override long Position { get;set; } = 0;

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return 0;
    }

    public override void SetLength(long value)
    {
        
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        
    }
}


public class ScreenStream : ITP3DataStream
{
    public uint Iounit => 0;

    public ulong Position => 0;

    public void Close()
    {
        
    }

    public async Task Open()
    {
        
    }

    public async Task<byte[]> Read(ulong offset, ulong maxCount)
    {
        return new byte[]{};
    }

    public async Task<ulong> Write(ulong offset, byte[] data)
    {
        return 0;
    }
}