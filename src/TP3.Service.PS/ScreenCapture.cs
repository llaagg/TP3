using TP3.Interfaces;
using TP3.Protocol;

public class WindowsScreenCaptureStream : BaseReadableStream
{
    private long _length;

    public WindowsScreenCaptureStream()
    {
        using var image = WG.CaptureWindow();
        using var ms = new MemoryStream();
        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png); 
        this._length = ms.Length;
    }

    override public long OnGetLength()
    {
        return this._length;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var position = this.Position;
        if(position >= this.Length)
        {
            return 0;
        }

        using var image = WG.CaptureWindow();
        using var ms = new MemoryStream();
        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png); 
        var bytes = ms.ToArray();
        var howmany = Math.Min(count, bytes.Length);
        Array.Copy(bytes, 0, buffer, offset, howmany);
        
        this.Position += howmany;
        return howmany;
    }
}