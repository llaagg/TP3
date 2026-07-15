using TP3.Interfaces;
using TP3.Protocol;

public class WindowsScreenCaptureStream : BaseReadableStream
{
    

    public override int Read(byte[] buffer, int offset, int count)
    {
        using var image = WG.CaptureWindow();
        using var ms = new MemoryStream();
        image.Save(ms, System.Drawing.Imaging.ImageFormat.Png); 
        var bytes = ms.ToArray();
        Array.Copy(bytes, 0, buffer, offset, Math.Min(count, bytes.Length));
        
        return Math.Min(count, bytes.Length);
    }
}