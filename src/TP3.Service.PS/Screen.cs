using TP3.Protocol;

namespace TP3.Service.PS;

public class Screen : StreamNode
{
    public Screen() : base(() =>
    {
        return new WindowsScreenCaptureStream();
    })
    {
    }
}
