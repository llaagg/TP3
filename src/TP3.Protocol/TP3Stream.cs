using TP3.Interfaces;

namespace TP3.Protocol;

public class TP3Stream: ITP3DataStream
{
    private const ulong DefaultMaxCount = 16 * 1024;
    private readonly Stream stream;
    private ulong logicalPosition;

    public TP3Stream(Stream stream)
    {
        this.stream = stream;
        if (stream.CanSeek)
        {
            this.logicalPosition = (ulong)stream.Position;
        }
    }

    public uint Iounit => 0;

    public ulong Position => stream.CanSeek ? (ulong)stream.Position : this.logicalPosition;

    public Task Open()
    {
        // No-op for a regular stream
        return Task.CompletedTask;
    }

    public async Task<byte[]> Read(ulong offset, ulong maxCount)
    {
        if (maxCount == 0)
        {
            maxCount = DefaultMaxCount;
        }

        if (stream.CanSeek)
        {
            if (offset != (ulong)stream.Position)
            {
                stream.Seek((long)offset, SeekOrigin.Begin);
            }
        }
        else if (offset != this.logicalPosition)
        {
            throw new NotSupportedException($"Non-seekable stream can only read at current position. Requested offset: {offset}, current position: {this.logicalPosition}.");
        }

        byte[] buffer = new byte[maxCount];
        int bytesRead = await stream.ReadAsync(buffer, 0, (int)maxCount);
        this.logicalPosition += (ulong)bytesRead;
        if (bytesRead < (int)maxCount)
        {
            Array.Resize(ref buffer, bytesRead);
        }
        return buffer;
    }

    public void Close()
    {
        stream.Close();
    }

    public async Task<ulong> Write(ulong offset, byte[] data)
    {
        if (stream.CanSeek)
        {
            if (offset != (ulong)stream.Position)
            {
                stream.Seek((long)offset, SeekOrigin.Begin);
            }
        }
        else if (offset != this.logicalPosition)
        {
            throw new NotSupportedException($"Non-seekable stream can only write at current position. Requested offset: {offset}, current position: {this.logicalPosition}.");
        }

        await stream.WriteAsync(data, 0, data.Length);
        this.logicalPosition += (ulong)data.Length;
        return (ulong)data.Length;
    }
}