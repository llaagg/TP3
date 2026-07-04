namespace TP3.Messages;

/// <summary>
/// Merge multiple messages and build stream of data
/// </summary>
public class TP3ReadResponseDataStream : Stream
{
    private readonly IEnumerator<TP3ReadResponse> _responses;
    private ReadOnlyMemory<byte> _currentChunk;
    private int _currentChunkOffset;
    private bool _isDisposed;

    public bool IsEmpty { get; }

    public TP3ReadResponseDataStream(IEnumerable<TP3ReadResponse> responses)
    {
        _responses = responses.GetEnumerator();
        IsEmpty = !MoveToNextChunk();
    }

    public override bool CanRead => !_isDisposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var totalCopied = 0;
        while (destination.Length > 0)
        {
            if (_currentChunkOffset >= _currentChunk.Length)
            {
                if (!MoveToNextChunk())
                {
                    break;
                }
            }

            var source = _currentChunk.Span[_currentChunkOffset..];
            var toCopy = Math.Min(source.Length, destination.Length);
            source[..toCopy].CopyTo(destination);

            destination = destination[toCopy..];
            _currentChunkOffset += toCopy;
            totalCopied += toCopy;
        }

        return totalCopied;
    }

    public override int ReadByte()
    {
        Span<byte> one = stackalloc byte[1];
        return Read(one) == 1 ? one[0] : -1;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        return ValueTask.FromResult(Read(buffer.Span));
    }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _responses.Dispose();
        }

        _isDisposed = true;
        base.Dispose(disposing);
    }

    private bool MoveToNextChunk()
    {
        while (_responses.MoveNext())
        {
            var bytes = _responses.Current.Data;
            if (bytes is null || bytes.IsEmpty)
            {
                continue;
            }

            _currentChunk = bytes.Memory;
            _currentChunkOffset = 0;
            return true;
        }

        return false;
    }
}