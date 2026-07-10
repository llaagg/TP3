namespace TP3.Service.Remote;

public sealed class PrefixStream : Stream
{
    private readonly byte[] prefix;
    private readonly Stream inner;
    private int prefixOffset;

    public PrefixStream(byte[] prefix = null!, Stream inner = null!)
    {
        this.prefix = prefix ?? Array.Empty<byte>();
        this.inner = inner ?? Stream.Null;
    }

    public override bool CanRead => inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (prefixOffset < prefix.Length)
        {
            var remaining = prefix.Length - prefixOffset;
            var toCopy = Math.Min(count, remaining);
            Buffer.BlockCopy(prefix, prefixOffset, buffer, offset, toCopy);
            prefixOffset += toCopy;
            return toCopy;
        }

        return inner.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (prefixOffset < prefix.Length)
        {
            var remaining = prefix.Length - prefixOffset;
            var toCopy = Math.Min(buffer.Length, remaining);
            prefix.AsMemory(prefixOffset, toCopy).CopyTo(buffer);
            prefixOffset += toCopy;
            return toCopy;
        }

        return await inner.ReadAsync(buffer, cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (prefixOffset < prefix.Length)
        {
            var remaining = prefix.Length - prefixOffset;
            var toCopy = Math.Min(count, remaining);
            Buffer.BlockCopy(prefix, prefixOffset, buffer, offset, toCopy);
            prefixOffset += toCopy;
            return Task.FromResult(toCopy);
        }

        return inner.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}