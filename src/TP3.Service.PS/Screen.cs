using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Service.PS;

public class Screen : INode
{
    private readonly WindowsScreenCaptureStream stream;
    public Screen()
    {
        this.stream = new WindowsScreenCaptureStream();
    }

    public string Id => "0";

    public string Name => "screen.png";

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => null;

    public ulong Length
    {
        get
        {            
            return (ulong)this.stream.Length;
        }
    }

    public async Task<ITP3DataStream?> Get()
    {
        return new TP3Stream(this.stream);
    }
}
