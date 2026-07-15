using TP3.Interfaces;
using TP3.Protocol;

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

public class ScreenCaptureStream : BaseReadableStream
{
    
}