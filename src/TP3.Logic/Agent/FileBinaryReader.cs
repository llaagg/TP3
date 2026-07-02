using TP3.Interfaces;

namespace TP3.Service.FileSystem;

internal sealed class FileBinaryReader : Reader
{
    private readonly string absolutePath;

    public FileBinaryReader(string absolutePath)
    {
        this.absolutePath = absolutePath;
    }

    public override async Task<ServiceReadResult> ReadAsync(ulong offset, uint maxBytes)
    {
        await using var stream = File.OpenRead(absolutePath);
        if (offset >= (ulong)stream.Length)
        {
            return new ServiceReadResult
            {
                Data = Array.Empty<byte>(),
                NextOffset = offset,
                IsEof = true
            };
        }

        stream.Seek((long)offset, SeekOrigin.Begin);
        var buffer = new byte[maxBytes];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
        if (bytesRead <= 0)
        {
            return new ServiceReadResult
            {
                Data = Array.Empty<byte>(),
                NextOffset = offset,
                IsEof = true
            };
        }

        var payload = new byte[bytesRead];
        Buffer.BlockCopy(buffer, 0, payload, 0, bytesRead);

        return new ServiceReadResult
        {
            Data = payload,
            NextOffset = offset + (ulong)bytesRead,
            IsEof = false
        };
    }
}


