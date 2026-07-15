using TP3.Interfaces;
using TP3.Messages;
using TP3.Protocol;

namespace TP3.Service.PS;

public class Screen : INode
{
    public Screen()
    {
        
    }

    public string Id => "0";

    public string Name => "screen";

    public NodeType NodeType => NodeType.File;

    public IEnumerable<INode>? Children => null;

    public async Task<ITP3DataStream?> Get()
    {
        var stream = new WindowsScreenCaptureStream();
        return new TP3Stream(stream);
    }
}
