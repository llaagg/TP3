using TP3.Interfaces;

namespace TP3.Protocol;

public class TP3Stream: ITP3DataStream
{
    private const ulong DefaultMaxCount = 16 * 1024;
    private readonly Stream stream;

    public TP3Stream(Stream stream)
    {
        this.stream = stream;
    }

    public uint Iounit => 0;

    public ulong Position => (ulong)stream.Position;

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

        if (offset != (ulong)stream.Position)
        {
            stream.Seek((long)offset, SeekOrigin.Begin);
        }

        byte[] buffer = new byte[maxCount];
        int bytesRead = await stream.ReadAsync(buffer, 0, (int)maxCount);
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

    public Task<ulong> Write(ulong offset, byte[] data)
    {
        if (offset != (ulong)stream.Position)
        {
            stream.Seek((long)offset, SeekOrigin.Begin);
        }

        stream.Write(data, 0, data.Length);
        return Task.FromResult((ulong)data.Length);
    }
}