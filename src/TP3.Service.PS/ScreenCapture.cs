using TP3.Interfaces;
using TP3.Protocol;
using Windows.Graphics.Capture;

public class WindowsScreenCaptureStream : BaseReadableStream
{
    public override int Read(byte[] buffer, int offset, int count)
    {
        return base.Read(buffer, offset, count);
    }
}